using BinarySerializer.Klonoa.KH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace KlonoaHeroesPatcher
{
    public class CutsceneFileEditorViewModel : FileEditorViewModel
    {
        public Cutscene_File CutsceneFile => (Cutscene_File)SerializableObject;

        public string Text { get; set; }
        public ImageSource TextPreviewImgSource { get; set; }
        public int TextPreviewWidth { get; set; }

        public string ScriptText { get; set; }

        protected override void Load()
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

            Text = txtStr.ToString();

            RefreshTextPreview();

            RefreshScripts();
        }
        protected override object GetEditor() => new CutsceneFileEditor();

        protected CutsceneTextCommand[] GetTextCommands() => CutsceneFile.Commands.First(x => x.Type == CutsceneCommand.CommandType.SetText).TextCommands;

        public string GetFontChar(int index)
        {
            return AppViewModel.Current.Config.FontTable.TryGetValue(index, out string v) ? v : $"[{index:X4}]";
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