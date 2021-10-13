using BinarySerializer.Klonoa.KH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace KlonoaHeroesPatcher
{
    public class CutsceneFileEditorViewModel : FileEditorViewModel
    {
        public CutsceneFileEditorViewModel()
        {
            ApplyTextChangesCommand = new RelayCommand(ApplyModifiedText);
        }

        public ICommand ApplyTextChangesCommand { get; }

        public Cutscene_File CutsceneFile => (Cutscene_File)SerializableObject;
        public CutsceneTextOnly_File CutsceneTextOnly { get; set; }

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

        public string ScriptText { get; set; }

        protected override void Load(bool firstLoad)
        {
            PendingTextChanges = false;

            if (firstLoad)
            {
                using (AppViewModel.Current.Context)
                {
                    var s = AppViewModel.Current.Context.Deserializer;
                    s.Goto(CutsceneFile.Offset);
                    var scriptsLength = CutsceneFile.Commands.First(x => x.Type == CutsceneCommand.CommandType.SetText).TextCommands.First().Offset.FileOffset - CutsceneFile.Offset.FileOffset;
                    CutsceneTextOnly = s.SerializeObject<CutsceneTextOnly_File>(default, x => x.Pre_ScriptsLength = scriptsLength, name: nameof(CutsceneTextOnly));
                }
            }

            RefreshText();
            RefreshTextPreview();
            RefreshScripts();
        }
        protected override object GetEditor() => new CutsceneFileEditor();

        protected CutsceneTextCommand[] GetTextCommands() => CutsceneTextOnly.TextCommands;

        public string GetFontChar(int index)
        {
            return AppViewModel.Current.Config.FontTable.TryGetValue(index, out string v) ? v : $"[0x{index:X4}]";
        }

        public void ApplyModifiedText()
        {
            Dictionary<int, string> fontTable = AppViewModel.Current.Config.FontTable;
            var textCmds = new List<CutsceneTextCommand>();

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

                            textCmds.Add(new CutsceneTextCommand
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

                            var cmdType = Enum.TryParse<CutsceneTextCommand.CommandType>(cmdName, true, out CutsceneTextCommand.CommandType t)
                                ? t
                                : CutsceneTextCommand.CommandType.None;

                            if (cmdType == CutsceneTextCommand.CommandType.None)
                            {
                                MessageBox.Show($"Invalid command {cmdStr}. The current changes will not be saved until all issues have been resolved.", "Error updating text commands", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            short arg = 0;

                            if (CutsceneTextCommand.HasArgument(cmdType))
                            {
                                if (argSeparatorIndex == -1)
                                {
                                    MessageBox.Show($"Command {cmdStr} requires an argument. The current changes will not be saved until all issues have been resolved.", "Error updating text commands", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                string argStr = cmdStr[(argSeparatorIndex + 1)..].Trim();
                                arg = Convert.ToInt16(argStr, 16);
                            }

                            textCmds.Add(new CutsceneTextCommand
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
                        foreach (var f in fontTable)
                        {
                            if (f.Value.Equals(c.ToString(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                fontIndex = f.Key;
                                break;
                            }
                        }

                        // If no match was found we assume the item might consist of multiple characters
                        if (fontIndex == -1)
                        {
                            foreach (var f in fontTable.Where(x => x.Value.Length > 1))
                            {
                                var check = Text.Substring(i, f.Value.Length);

                                if (check.Equals(f.Value, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    fontIndex = f.Key;
                                    i += f.Value.Length - 1;
                                    break;
                                }
                            }
                        }

                        // If there is still no match we return
                        if (fontIndex == -1)
                        {
                            MessageBox.Show($"The character '{c}' has not been defined as a valid character. The current changes will not be saved until all invalid characters have been removed.", "Error updating text commands", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        textCmds.Add(new CutsceneTextCommand
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

            CutsceneTextOnly.TextCommands = textCmds.ToArray();

            // Relocate the data
            RelocateFile(CutsceneTextOnly);

            // Reload
            Load(false);
        }

        public void RefreshText()
        {
            CutsceneTextCommand[] txtCmds = GetTextCommands();
            var txtStr = new StringBuilder();

            foreach (CutsceneTextCommand cmd in txtCmds)
            {
                if (cmd.IsCommand)
                {
                    var newLine = cmd.Command is CutsceneTextCommand.CommandType.Clear or CutsceneTextCommand.CommandType.Linebreak;

                    if (newLine)
                        txtStr.AppendLine();

                    txtStr.Append("[");
                    txtStr.Append(cmd.Command.ToString().ToUpper());

                    if (CutsceneTextCommand.HasArgument(cmd.Command))
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
            CutsceneTextCommand[] txtCmds = GetTextCommands();

            // Split based on lines and remove commands
            var txtLines = new List<List<CutsceneTextCommand>>()
            {
                new List<CutsceneTextCommand>()
            };

            foreach (CutsceneTextCommand cmd in txtCmds)
            {
                if (cmd.IsCommand)
                {
                    if (cmd.Command is CutsceneTextCommand.CommandType.Clear or CutsceneTextCommand.CommandType.Linebreak)
                        txtLines.Add(new List<CutsceneTextCommand>());
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
                palette: font.Palette.Skip(16 * 3).ToArray(), // Use palette 3
                tileMap: map,
                width: width * TileGraphicsHelpers.TileWidth,
                height: height * TileGraphicsHelpers.TileHeight,
                trimPalette: false);

            // Display at twice the size
            TextPreviewWidth = width * TileGraphicsHelpers.TileWidth * 2;
        }

        public void RefreshScripts()
        {
            var scriptStr = new StringBuilder();

            foreach (CutsceneCommand cmd in CutsceneFile.Commands)
            {
                var s = new CutsceneCommandsGetArgsSerializerObject(cmd.Context);
                cmd.SerializeImpl(s);

                appendScriptLine(1, cmd);

                if (cmd.SubCommands != null)
                    foreach (CutsceneCommand subCmd in cmd.SubCommands)
                        appendScriptLine(2, subCmd);
            }

            ScriptText = scriptStr.ToString();

            void appendScriptLine(int indentLevel, CutsceneCommand cmd)
            {
                var s = new CutsceneCommandsGetArgsSerializerObject(cmd.Context);
                cmd.SerializeImpl(s);

                scriptStr.AppendLine(
                    $"{cmd.Offset.StringAbsoluteOffset}:" +
                    $"{new string('\t', indentLevel)}" +
                    $"{cmd.Type.ToString().ToUpper()}({String.Join(", ", s.Arguments.Skip(2).Select(x => $"{x.Name}: {x.Value}"))})");
            }
        }
    }
}