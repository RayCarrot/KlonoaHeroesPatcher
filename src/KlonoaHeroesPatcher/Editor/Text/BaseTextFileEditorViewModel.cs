using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BinarySerializer.Klonoa.KH;

namespace KlonoaHeroesPatcher
{
    public abstract class BaseTextFileEditorViewModel : FileEditorViewModel
    {
        protected BaseTextFileEditorViewModel()
        {
            ApplyTextChangesCommand = new RelayCommand(ApplyModifiedText);

            AllowedCommandsInfo = new ObservableCollection<DuoGridItemViewModel>()
            {
                new DuoGridItemViewModel("[END]", "End of the cutscene text. Should always be placed as the last command."),
                new DuoGridItemViewModel("[CLEAR]", "Clears the previously drawn text."),
                new DuoGridItemViewModel("[LINEBREAK]", "Draws the text on a new line."),
                new DuoGridItemViewModel("[SPEAKER:X]", "Draws the text for the speaker's name, specified with the argument."),
                new DuoGridItemViewModel("[CMD_05]", "Unknown"),
                new DuoGridItemViewModel("[CMD_06]", "Unknown"),
                new DuoGridItemViewModel("[CMD_07:X]", "Unknown"),
                new DuoGridItemViewModel("[CMD_08:X]", "Unknown"),
                new DuoGridItemViewModel("[PROMPT]", "Waits for the player to press a button before continuing."),
                new DuoGridItemViewModel("[PAUSE]", "Unknown"),
                new DuoGridItemViewModel("[CMD_0B]", "Unknown"),
                new DuoGridItemViewModel("[CMD_0C:X]", "Unknown"),
                new DuoGridItemViewModel("[CMD_0D:X]", "Unknown"),
                new DuoGridItemViewModel("[CMD_0E]", "Unknown"),
                new DuoGridItemViewModel("[CMD_0F]", "Unknown"),
            };
        }

        public ICommand ApplyTextChangesCommand { get; }

        public ObservableCollection<DuoGridItemViewModel> AllowedCharactersInfo { get; set; }
        public ObservableCollection<DuoGridItemViewModel> AllowedCommandsInfo { get; }

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

        public ImageSource TextPreviewImgSource { get; set; }
        public int TextPreviewWidth { get; set; }

        protected override void Load(bool firstLoad)
        {
            PendingTextChanges = false;

            if (firstLoad)
                AllowedCharactersInfo = new ObservableCollection<DuoGridItemViewModel>(AppViewModel.Current.Config.FontTable.Select(x => new DuoGridItemViewModel($"{x.Key:X4}", String.Join(", ", x.Value))));

            RefreshText();
            RefreshTextPreview();
        }

        protected abstract TextCommand[] GetTextCommands();
        protected abstract void RelocateTextCommands(TextCommand[] cmds);

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

            RelocateTextCommands(textCmds.ToArray());

            // Reload
            Load(false);
        }

        public void RefreshText()
        {
            TextCommand[] txtCmds = GetTextCommands();
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

        public void RefreshTextPreview()
        {
            // Get the font
            Graphics_File font = AppViewModel.Current.ROM.UIPack.Font_0;

            // Get the text commands
            TextCommand[] txtCmds = GetTextCommands();

            // Split based on lines and remove commands
            var txtLines = new List<List<TextCommand>>()
            {
                new List<TextCommand>()
            };

            foreach (TextCommand cmd in txtCmds)
            {
                if (cmd.IsCommand)
                {
                    if (cmd.Command is TextCommand.CommandType.Clear or TextCommand.CommandType.Linebreak)
                        txtLines.Add(new List<TextCommand>());
                }
                else
                {
                    txtLines.Last().Add(cmd);
                }
            }

            // Create a tile map
            var width = txtLines.Max(x => x.Count);
            var height = txtLines.Count * 2;
            var map = new GraphicsTile[width * height];

            for (int y = 0; y < height; y += 2)
            {
                for (int x = 0; x < txtLines[y / 2].Count; x++)
                {
                    var fontIndex = txtLines[y / 2][x].FontIndex;

                    var fontWidth = font.TileMapWidth / TileGraphicsHelpers.TileWidth;
                    var fontX = fontIndex % fontWidth;
                    var fontY = fontIndex / fontWidth;
                    fontY *= 2;

                    map[y * width + x] = new GraphicsTile()
                    {
                        TileSetIndex = fontY * fontWidth + fontX,
                    };
                    map[(y + 1) * width + x] = new GraphicsTile()
                    {
                        TileSetIndex = (fontY + 1) * fontWidth + fontX,
                    };
                }
            }

            TextPreviewImgSource = TileGraphicsHelpers.CreateImageSource(
                tileSet: font.TileSet,
                bpp: font.BPP,
                palette: font.Palette,
                tileMap: map,
                width: width * TileGraphicsHelpers.TileWidth,
                height: height * TileGraphicsHelpers.TileHeight,
                basePalette: 3);

            // Display at twice the size
            TextPreviewWidth = width * TileGraphicsHelpers.TileWidth * 2;
        }
    }
}