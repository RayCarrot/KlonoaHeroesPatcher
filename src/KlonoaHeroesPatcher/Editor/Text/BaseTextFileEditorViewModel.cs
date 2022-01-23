using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BinarySerializer.Klonoa.KH;

namespace KlonoaHeroesPatcher;

public abstract class BaseTextFileEditorViewModel : FileEditorViewModel
{
    static BaseTextFileEditorViewModel()
    {
        AllowedCharactersInfo = new ObservableCollection<DuoGridItemViewModel>(AppViewModel.Current.Config.FontTable.Select(x => new DuoGridItemViewModel($"{x.Key:X4}", String.Join(", ", x.Value))));

        AllowedCommandsInfo = new ObservableCollection<DuoGridItemViewModel>()
        {
            new DuoGridItemViewModel("[END]", "End of the cutscene text. Should always be placed as the last command."),
            new DuoGridItemViewModel("[CLEAR]", "Clears the previously drawn text."),
            new DuoGridItemViewModel("[LINEBREAK]", "Draws the text on a new line."),
            new DuoGridItemViewModel("[SPEAKER:X]", "Draws the text for the speaker's name, specified with the argument."),
            new DuoGridItemViewModel("[CMD_05]", "Unknown"),
            new DuoGridItemViewModel("[CMD_06]", "Unknown"),
            new DuoGridItemViewModel("[CMD_07:X]", "Unknown"),
            new DuoGridItemViewModel("[BLANKSPACE:X]", "Adds blank with the length specified with the argument. If 0 then it defaults to 0x28."),
            new DuoGridItemViewModel("[PROMPT]", "Waits for the player to press a button before continuing."),
            new DuoGridItemViewModel("[PAUSE]", "Unknown"),
            new DuoGridItemViewModel("[CMD_0B]", "Unknown"),
            new DuoGridItemViewModel("[Speed:X]", "Sets the text speed where the argument is the speed in frames."),
            new DuoGridItemViewModel("[CMD_0D:X]", "Unknown"),
            new DuoGridItemViewModel("[CMD_0E]", "Unknown"),
            new DuoGridItemViewModel("[CMD_0F]", "Unknown"),
        };
    }

    protected BaseTextFileEditorViewModel()
    {
        KlonoaHeroesROM rom = AppViewModel.Current.ROM;

        TextPreviewFonts = new ObservableCollection<TextPreviewFontViewModel>()
        {
            new TextPreviewFontViewModel(nameof(rom.UIPack.Font_0), rom.UIPack.Font_0, true),
            new TextPreviewFontViewModel(nameof(rom.UIPack.Font_1), rom.UIPack.Font_1, false),
        };

        _selectedTextPreviewFont = TextPreviewFonts.First();
    }

    public static ObservableCollection<DuoGridItemViewModel> AllowedCharactersInfo { get; }
    public static ObservableCollection<DuoGridItemViewModel> AllowedCommandsInfo { get; }

    public ObservableCollection<TextItemViewModel> TextItems { get; set; }
    public TextItemViewModel SelectedTextItem { get; set; }
    public bool HasMultipleTextItems { get; set; }

    public ObservableCollection<TextPreviewFontViewModel> TextPreviewFonts { get; }

    private TextPreviewFontViewModel _selectedTextPreviewFont;
    public TextPreviewFontViewModel SelectedTextPreviewFont
    {
        get => _selectedTextPreviewFont;
        set
        {
            _selectedTextPreviewFont = value;

            foreach (TextItemViewModel textItem in TextItems)
                textItem.RefreshTextPreview(SelectedTextPreviewFont);
        }
    }

    public override void Load(bool firstLoad)
    {
        if (firstLoad)
        {
            TextItems = new ObservableCollection<TextItemViewModel>(GetTextCommandViewModels());

            foreach (TextItemViewModel textItem in TextItems)
            {
                textItem.RefreshText();
                textItem.PendingTextChanges = false;
            }

            SelectedTextItem = TextItems.First();
            HasMultipleTextItems = TextItems.Count > 1;
        }

        foreach (TextItemViewModel textItem in TextItems)
            textItem.RefreshTextPreview(SelectedTextPreviewFont);
    }

    public override void Unload()
    {
        foreach (TextItemViewModel textItem in TextItems)
            textItem.TextPreviewImages = null;
    }

    protected abstract IEnumerable<TextItemViewModel> GetTextCommandViewModels();
    protected virtual void RelocateTextCommands() => RelocateFile();

    public class TextItemViewModel : BaseViewModel
    {
        #region Constructor

        public TextItemViewModel(BaseTextFileEditorViewModel editorViewModel, TextCommands textCommands, string displayName)
        {
            EditorViewModel = editorViewModel;
            TextCommands = textCommands ?? throw new ArgumentNullException(nameof(textCommands));
            DisplayName = displayName;

            ApplyTextChangesCommand = new RelayCommand(ApplyModifiedText);
        }

        #endregion

        #region Commands

        public ICommand ApplyTextChangesCommand { get; }

        #endregion

        #region Public Properties

        public BaseTextFileEditorViewModel EditorViewModel { get; }
        public TextCommands TextCommands { get; }
        public string DisplayName { get; }

        private string _text;
        public string Text
        {
            get => _text;
            set
            {
                if (value == Text)
                    return;

                _text = value;
                PendingTextChanges = true;
            }
        }
        public bool PendingTextChanges { get; set; }

        public ObservableCollection<ImageSource> TextPreviewImages { get; set; }
        public int TextPreviewWidth { get; set; }

        #endregion

        #region Public Methods

        public string GetFontChar(int index)
        {
            var a = AppViewModel.Current.Config.FontTable.TryGetValue(index, out string[] v) ? v : null;

            return a?.FirstOrDefault() ?? $"[0x{index:X4}]";
        }

        public void ApplyModifiedText()
        {
            Dictionary<int, string[]> fontTable = AppViewModel.Current.Config.FontTable;
            var textCmds = new List<TextCommand>();

            try
            {
                for (int i = 0; i < Text.Length; i++)
                {
                    char c = Text[i];

                    // Ignore linebreaks
                    if (c is '\n' or '\r')
                        continue;

                    // Special case
                    if (c == '[')
                    {
                        int endIndex = Text.IndexOf(']', i);

                        if (endIndex == -1)
                        {
                            MessageBox.Show($"No closing bracket found for bracket at character {i}. The current changes will not be saved until all issues have been resolved.", "Error updating text commands", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        int cmdLength = endIndex - i - 1;
                        string cmdStr = Text.Substring(i + 1, cmdLength);

                        // Character not in font table
                        if (cmdStr.StartsWith("0x"))
                        {
                            string hexStr = cmdStr[2..];
                            int fontIndex = Convert.ToInt32(hexStr, 16);

                            textCmds.Add(new TextCommand
                            {
                                FontIndex = (short)fontIndex,
                            });
                        }
                        // Command
                        else
                        {
                            int argSeparatorIndex = cmdStr.IndexOf(':');
                            string cmdName = cmdStr;

                            if (argSeparatorIndex != -1)
                                cmdName = cmdName[..argSeparatorIndex];

                            var cmdType = Enum.TryParse<TextCommand.CommandType>(cmdName, true, out TextCommand.CommandType t)
                                ? t
                                : TextCommand.CommandType.None;

                            if (cmdType == TextCommand.CommandType.None)
                            {
                                MessageBox.Show($"Invalid command {cmdStr}. The current changes will not be saved until all issues have been resolved.", "Error updating text commands", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            short arg = 0;

                            if (TextCommand.HasArgument(cmdType))
                            {
                                if (argSeparatorIndex == -1)
                                {
                                    MessageBox.Show($"Command {cmdStr} requires an argument. The current changes will not be saved until all issues have been resolved.", "Error updating text commands", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                string argStr = cmdStr[(argSeparatorIndex + 1)..].Trim();
                                arg = Convert.ToInt16(argStr, 16);
                            }

                            textCmds.Add(new TextCommand
                            {
                                FontIndex = (short)cmdType,
                                CommandArgument = arg,
                            });

                            if (cmdType == TextCommand.CommandType.End)
                                break;
                        }

                        i += cmdLength + 1;
                    }
                    else
                    {
                        int fontIndex = -1;

                        // Start by checking if it fully matches an item in the font table
                        foreach (KeyValuePair<int, string[]> f in fontTable)
                        {
                            if (f.Value.Any(s => s.Equals(c.ToString(), AppViewModel.Current.Config.TextStringComparison)))
                            {
                                fontIndex = f.Key;
                                break;
                            }
                        }

                        // If no match was found we assume the item might consist of multiple characters
                        if (fontIndex == -1)
                        {
                            foreach (KeyValuePair<int, string[]> f in fontTable)
                            {
                                foreach (string s in f.Value.Where(x => x.Length > 1))
                                {
                                    var check = Text.Substring(i, f.Value.Length);

                                    if (check.Equals(s, AppViewModel.Current.Config.TextStringComparison))
                                    {
                                        fontIndex = f.Key;
                                        i += f.Value.Length - 1;
                                        break;
                                    }
                                }

                                if (fontIndex != -1)
                                    break;
                            }
                        }

                        // If there is still no match we return
                        if (fontIndex == -1)
                        {
                            MessageBox.Show($"The character '{c}' has not been defined as a valid character. The current changes will not be saved until all invalid characters have been removed.", "Error updating text commands", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        textCmds.Add(new TextCommand
                        {
                            FontIndex = (short)fontIndex,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred. The current changes will not be saved until all issues have been resolved. Error: {ex.Message}", "Error updating text commands", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (textCmds.LastOrDefault()?.Command != TextCommand.CommandType.End)
                textCmds.Add(new TextCommand()
                {
                    FontIndex = (short)TextCommand.CommandType.End
                });

            if (TextCommands.Pre_MaxLength != null && textCmds.Count > TextCommands.Pre_MaxLength)
            {
                var diff = textCmds.Count - (int)TextCommands.Pre_MaxLength;
                textCmds.RemoveRange(textCmds.Count - diff - 1, diff);
            }

            TextCommands.Commands = textCmds.ToArray();

            EditorViewModel.RelocateTextCommands();

            // Reload
            EditorViewModel.Load(true);
        }

        public void RefreshText()
        {
            TextCommand[] txtCmds = TextCommands.Commands;
            var txtStr = new StringBuilder();

            foreach (TextCommand cmd in txtCmds)
            {
                if (cmd.IsCommand)
                {
                    var newLine = cmd.Command is TextCommand.CommandType.Clear or TextCommand.CommandType.Linebreak;

                    txtStr.Append("[");
                    txtStr.Append(cmd.Command.ToString().ToUpper());

                    if (TextCommand.HasArgument(cmd.Command))
                        txtStr.Append($": {cmd.CommandArgument:X4}");

                    txtStr.Append("]");

                    if (newLine)
                        txtStr.AppendLine();
                }
                else
                {
                    txtStr.Append(GetFontChar(cmd.FontIndex));
                }
            }

            _text = txtStr.ToString();
            OnPropertyChanged(nameof(Text));
        }

        public int GetTextPreviewsMaxWidth()
        {
            int maxWidth = 0;
            int startXPos = 0;
            int xPos = startXPos;

            foreach (TextCommand cmd in TextCommands.Commands)
            {
                if (cmd.IsCommand)
                {
                    switch (cmd.Command)
                    {
                        case TextCommand.CommandType.End:
                        case TextCommand.CommandType.Clear:
                        case TextCommand.CommandType.Linebreak:
                            if (xPos > maxWidth)
                                maxWidth = xPos;

                            if (cmd.Command == TextCommand.CommandType.Clear)
                                startXPos = 0;
                                
                            xPos = startXPos;
                            break;

                        case TextCommand.CommandType.Speaker:
                            xPos += 0x28;
                            startXPos = 0x28;
                            break;

                        case TextCommand.CommandType.BlankSpace:
                            xPos += cmd.CommandArgument == 0 ? 0x28 : cmd.CommandArgument;
                            break;
                    }
                }
                else
                {
                    xPos += 8;
                }
            }

            return maxWidth;
        }

        public void RefreshTextPreview(TextPreviewFontViewModel fontViewModel)
        {
            try
            {
                // Get the rom and font
                KlonoaHeroesROM rom = AppViewModel.Current.ROM;
                Graphics_File font = fontViewModel.Font;

                // Get the text commands
                TextCommand[] txtCmds = TextCommands.Commands;

                byte[] cutomWidths = AppViewModel.Current.PatchViewModels.Select(x => x.Patch).OfType<VariableWidthFontPatch>().FirstOrDefault()?.Widths;

                const int marginX = 8;
                const int marginY = 8;

                // Get the format
                var bpp = font.BPP;
                PixelFormat format = PixelFormats.Indexed8;
                float bppFactor = bpp / 8f;

                // Get the length of each tile in bytes
                int tileLength = (int)(TileGraphicsHelpers.TileWidth * TileGraphicsHelpers.TileHeight * bppFactor);

                // Get the palette
                BitmapPalette bmpPal = new BitmapPalette(ColorHelpers.ConvertColors(font.Palette, bpp, false));

                // Get the max width to use for all images
                int width = marginX * 2 + GetTextPreviewsMaxWidth();

                // Display at double the size in the UI
                TextPreviewWidth = width * 2;

                TextPreviewImages = new ObservableCollection<ImageSource>();

                int cmdIndex = 0;
                while (cmdIndex < txtCmds.Length)
                {
                    // Ignore if only the end command
                    if (cmdIndex + 1 == txtCmds.Length && txtCmds.Last().Command == TextCommand.CommandType.End)
                        break;

                    int startXPos = marginX;
                    int xPos = startXPos;
                    int yPos = marginY;

                    // Get the dimensions
                    int height = (txtCmds.
                        Skip(cmdIndex).
                        TakeWhile(x => x.Command != TextCommand.CommandType.Clear).
                        Count(x => x.Command is TextCommand.CommandType.Linebreak or TextCommand.CommandType.Clear) + 1) 
                        * TileGraphicsHelpers.TileHeight;

                    if (fontViewModel.UsesDoubleHeight)
                        height *= 2;

                    // Add a margin to the height so it's not right against the top and bottom edges
                    height += marginY * 2;

                    // Create a buffer for the image data
                    var imgData = new byte[width * height];

                    while (cmdIndex < txtCmds.Length)
                    {
                        TextCommand cmd = txtCmds[cmdIndex];

                        if (cmd.Command == TextCommand.CommandType.Clear)
                        {
                            cmdIndex++;
                            break;
                        }

                        if (cmd.IsCommand)
                        {
                            switch (cmd.Command)
                            {
                                case TextCommand.CommandType.Clear:
                                case TextCommand.CommandType.Linebreak:

                                    if (cmd.Command == TextCommand.CommandType.Clear)
                                        startXPos = marginX;

                                    xPos = startXPos;
                                    yPos += TileGraphicsHelpers.TileHeight;

                                    if (fontViewModel.UsesDoubleHeight)
                                        yPos += TileGraphicsHelpers.TileHeight;

                                    break;

                                case TextCommand.CommandType.Speaker:
                                    if (!fontViewModel.UsesDoubleHeight)
                                        break;

                                    if (cmd.CommandArgument <= 0x44)
                                    {
                                        Graphics_File speakerGraphics = rom.StoryPack.Speakers.Files[cmd.CommandArgument];

                                        for (int y = 0; y < 2; y++)
                                        {
                                            for (int x = 0; x < 4; x++)
                                            {
                                                TileGraphicsHelpers.DrawTileTo8BPPImg(
                                                    tileSet: speakerGraphics.TileSet,
                                                    tileSetOffset: tileLength * (y * 4 + x),
                                                    tileSetBpp: speakerGraphics.BPP,
                                                    paletteOffset: 3 * 16, // Use palette 3
                                                    flipX: false,
                                                    flipY: false,
                                                    imgData: imgData,
                                                    xPos: xPos + (x * TileGraphicsHelpers.TileWidth),
                                                    yPos: yPos + (y * TileGraphicsHelpers.TileHeight),
                                                    imgWidth: width);
                                            }
                                        }
                                    }

                                    xPos += 0x28;
                                    startXPos = marginX + 0x28;
                                    break;

                                case TextCommand.CommandType.BlankSpace:
                                    xPos += cmd.CommandArgument == 0 ? 0x28 : cmd.CommandArgument;
                                    break;
                            }
                        }
                        else
                        {
                            var fontWidth = font.TileMapWidth / TileGraphicsHelpers.TileWidth;
                            var fontX = cmd.FontIndex % fontWidth;
                            var fontY = cmd.FontIndex / fontWidth;

                            if (fontViewModel.UsesDoubleHeight)
                                fontY *= 2;

                            var tileIndexTop = fontY * fontWidth + fontX;
                            var tileIndexBottom = (fontY + 1) * fontWidth + fontX;

                            TileGraphicsHelpers.DrawTileTo8BPPImg(
                                tileSet: font.TileSet,
                                tileSetOffset: tileLength * tileIndexTop,
                                tileSetBpp: bpp,
                                paletteOffset: 3 * 16, // Use palette 3
                                flipX: false,
                                flipY: false,
                                imgData: imgData,
                                xPos: xPos,
                                yPos: yPos,
                                imgWidth: width);

                            if (fontViewModel.UsesDoubleHeight)
                                TileGraphicsHelpers.DrawTileTo8BPPImg(
                                    tileSet: font.TileSet,
                                    tileSetOffset: tileLength * tileIndexBottom,
                                    tileSetBpp: bpp,
                                    paletteOffset: 3 * 16, // Use palette 3
                                    flipX: false,
                                    flipY: false,
                                    imgData: imgData,
                                    xPos: xPos,
                                    yPos: yPos + TileGraphicsHelpers.TileHeight,
                                    imgWidth: width);

                            if (cutomWidths != null && cmd.FontIndex < cutomWidths.Length)
                                xPos += cutomWidths[cmd.FontIndex];
                            else
                                xPos += 8;
                        }

                        cmdIndex++;
                    }

                    BitmapSource img = BitmapSource.Create(
                        pixelWidth: width, 
                        pixelHeight: height, 
                        dpiX: TileGraphicsHelpers.DpiX, 
                        dpiY: TileGraphicsHelpers.DpiY, 
                        pixelFormat: format, 
                        palette: bmpPal, 
                        pixels: imgData, 
                        stride: TileGraphicsHelpers.GetStride(width, format));

                    TextPreviewImages.Add(img);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred refreshing the text preview. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                TextPreviewImages = null;
                TextPreviewWidth = 0;
            }
        }

        #endregion
    }

    public class TextPreviewFontViewModel : BaseViewModel
    {
        public TextPreviewFontViewModel(string displayName, Graphics_File font, bool usesDoubleHeight)
        {
            DisplayName = displayName;
            Font = font;
            UsesDoubleHeight = usesDoubleHeight;
        }

        public string DisplayName { get; }
        public Graphics_File Font { get; }
        public bool UsesDoubleHeight { get; }
    }
}