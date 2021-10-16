using BinarySerializer.Klonoa.KH;
using System;
using System.Linq;
using System.Text;

namespace KlonoaHeroesPatcher
{
    public class CutsceneFileEditorViewModel : BaseTextFileEditorViewModel
    {
        public Cutscene_File CutsceneFile => (Cutscene_File)SerializableObject;
        public CutsceneTextOnly_File CutsceneTextOnly { get; set; }
        
        public string ScriptText { get; set; }

        protected override void Load(bool firstLoad)
        {
            if (firstLoad)
            {
                using (AppViewModel.Current.Context)
                {
                    var s = AppViewModel.Current.Context.Deserializer;
                    s.Goto(CutsceneFile.Offset);
                    var scriptsLength = CutsceneFile.Commands.First(x => x.Type == CutsceneCommand.CommandType.SetText).TextCommands.Commands.First().Offset.FileOffset - CutsceneFile.Offset.FileOffset;
                    CutsceneTextOnly = s.SerializeObject<CutsceneTextOnly_File>(default, x => x.Pre_ScriptsLength = scriptsLength, name: nameof(CutsceneTextOnly));
                }
            }

            base.Load(firstLoad);

            RefreshScripts();
        }

        protected override TextCommand[] GetTextCommands() => CutsceneTextOnly.TextCommands;

        protected override void RelocateTextCommands(TextCommand[] cmds)
        {
            CutsceneTextOnly.TextCommands = cmds;

            // Relocate the data
            RelocateFile(CutsceneTextOnly);
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