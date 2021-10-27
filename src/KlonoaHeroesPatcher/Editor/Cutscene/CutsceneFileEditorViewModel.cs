using BinarySerializer.Klonoa.KH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BinarySerializer;

namespace KlonoaHeroesPatcher
{
    public class CutsceneFileEditorViewModel : BaseTextFileEditorViewModel
    {
        public Cutscene_File CutsceneFile => (Cutscene_File)SerializableObject;
        
        public string ScriptText { get; set; }

        protected override void Load(bool firstLoad)
        {
            base.Load(firstLoad);

            RefreshScripts();
        }

        protected override IEnumerable<TextItemViewModel> GetTextCommandViewModels()
        {
            foreach (CutsceneCommand cmd in CutsceneFile.Commands)
            {
                if (cmd.TextCommands != null)
                    yield return new TextItemViewModel(this, cmd.TextCommands, getRelativeOffset(cmd.TextCommands.Offset));

                if (cmd.TextCommandsArray != null)
                {
                    for (var i = 0; i < cmd.TextCommandsArray.Files.Length; i++)
                    {
                        TextCommands txtCmd = cmd.TextCommandsArray.Files[i];

                        if (txtCmd == null)
                            continue;

                        yield return new TextItemViewModel(this, txtCmd, $"{getRelativeOffset(txtCmd.Offset)}[{i}]");
                    }
                }
            }

            string getRelativeOffset(Pointer offset) => $"0x{offset.FileOffset - CutsceneFile.Offset.FileOffset:X4}";
        }

        protected override void RelocateTextCommands()
        {
            // TODO: Update command offsets
            throw new NotImplementedException();

            // Relocate the data
            RelocateFile();
        }

        public void RefreshScripts()
        {
            var scriptStr = new StringBuilder();

            foreach (CutsceneCommand cmd in CutsceneFile.Commands)
            {
                var s = new CutsceneCommandsGetArgsSerializerObject(cmd.Context);
                cmd.SerializeImpl(s);

                appendScriptLine(1, cmd);
            }

            ScriptText = scriptStr.ToString();

            void appendScriptLine(int indentLevel, CutsceneCommand cmd)
            {
                var s = new CutsceneCommandsGetArgsSerializerObject(cmd.Context);
                cmd.SerializeImpl(s);

                long relativeOffset = cmd.Offset.FileOffset - CutsceneFile.Offset.FileOffset;

                scriptStr.AppendLine(
                    $"{relativeOffset:X8}:" +
                    $"{new string('\t', indentLevel)}" +
                    $"{cmd.Type.ToString().ToUpper()}({String.Join(", ", s.Arguments.Skip(2).Select(x => $"{x.Name}: {x.Value}"))})");
            }
        }
    }
}