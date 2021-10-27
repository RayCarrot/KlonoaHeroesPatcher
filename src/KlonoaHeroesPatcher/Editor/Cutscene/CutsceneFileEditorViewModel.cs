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
                switch (cmd.Type)
                {
                    case CutsceneCommand.CommandType.End_0:
                    case CutsceneCommand.CommandType.End_1:
                        writeLine("END");
                        scriptStr.AppendLine();
                        break;

                    case CutsceneCommand.CommandType.Wait:
                        writeLine($"WAIT {cmd.Frames} FRAMES");
                        break;

                    case CutsceneCommand.CommandType.Call:
                        writeLine($"CALL {getFileOffsetString(cmd.GetPointerFromOffset(cmd.CommandOffset1))}");
                        break;

                    case CutsceneCommand.CommandType.Return:
                        writeLine($"RETURN");
                        scriptStr.AppendLine();
                        break;

                    case CutsceneCommand.CommandType.GoTo:
                        writeLine($"GOTO {getFileOffsetString(cmd.GetPointerFromOffset(cmd.CommandOffset1))}");
                        scriptStr.AppendLine();
                        break;

                    case CutsceneCommand.CommandType.ConditionalGoTo_0:
                        writeConditionLine($"0x{cmd.Arg4_Uint:X8}");
                        writeLine($"GOTO {getFileOffsetString(cmd.GetPointerFromOffset(cmd.CommandOffset1))}", 2, false);
                        break;

                    case CutsceneCommand.CommandType.SetText:
                        writeLine($"SET TEXT {getFileOffsetString(cmd.TextCommands.Offset)}");
                        break;

                    case CutsceneCommand.CommandType.UnknownEndOfFrame:
                        writeLine($"WAIT 1 FRAME");
                        break;

                    case CutsceneCommand.CommandType.ConditionalGoTo_1:
                    case CutsceneCommand.CommandType.ConditionalGoTo_2:
                    case CutsceneCommand.CommandType.ConditionalGoTo_3:
                        writeConditionLine($"0x{cmd.Arg2_Short:X4}");
                        writeLine($"GOTO {getFileOffsetString(cmd.GetPointerFromOffset(cmd.CommandOffset1))}", 2, false);
                        break;

                    case CutsceneCommand.CommandType.FileReference:
                        writeLine($"FILE REF {cmd.FileIndex_0}-{cmd.FileIndex_1}-{cmd.FileIndex_2}");
                        break;
                    
                    case CutsceneCommand.CommandType.Blank_0:
                    case CutsceneCommand.CommandType.Blank_1:
                    case CutsceneCommand.CommandType.Blank_2:
                    case CutsceneCommand.CommandType.Blank_3:
                    case CutsceneCommand.CommandType.Blank_4:
                    case CutsceneCommand.CommandType.Blank_5:
                    case CutsceneCommand.CommandType.Blank_6:
                    case CutsceneCommand.CommandType.Blank_7:
                    case CutsceneCommand.CommandType.Blank_8:
                    case CutsceneCommand.CommandType.Blank_9:
                    case CutsceneCommand.CommandType.Blank_10:
                    case CutsceneCommand.CommandType.Blank_11:
                    case CutsceneCommand.CommandType.Blank_12:
                    case CutsceneCommand.CommandType.Blank_13:
                    case CutsceneCommand.CommandType.Blank_14:
                    case CutsceneCommand.CommandType.Blank_15:
                        writeLine($"NOP");
                        break;

                    case CutsceneCommand.CommandType.ConditionalMultiGoTo:
                        writeLine($"GOTO {getFileOffsetString(cmd.GetPointerFromOffset(cmd.CommandOffset1))} or {getFileOffsetString(cmd.GetPointerFromOffset(cmd.CommandOffset2))} or NOP");
                        break;

                    case CutsceneCommand.CommandType.ConditionalGoTo_4:
                        writeConditionLine(null);
                        writeLine($"GOTO {getFileOffsetString(cmd.GetPointerFromOffset(cmd.CommandOffset1))}", 2, false);
                        break;

                    case CutsceneCommand.CommandType.SetTextMulti:
                        writeLine($"SET TEXT {getFileOffsetString(cmd.TextCommandsArray.Offset)}[{cmd.DefaultTextIndex}]");
                        break;

                    default:
                        var s = new CutsceneCommandsGetArgsSerializerObject(cmd.Context);
                        cmd.SerializeImpl(s);

                        var args = String.Join(", ", s.Arguments.Skip(2).Select(x =>
                        {
                            if (x.Value is short argShort)
                            {
                                if (x.Name is nameof(CutsceneCommand.CommandOffset1) or nameof(CutsceneCommand.CommandOffset2))
                                    return $"{getFileOffsetString(cmd.GetPointerFromOffset(argShort))}";
                                else
                                    return $"0x{argShort:X4}";
                            }
                            else if (x.Value is uint argUint)
                            {
                                return $"0x{argUint:X8}";
                            }
                            else if (x.Value is bool argBool)
                            {
                                return argBool.ToString().ToUpper();
                            }
                            else
                            {
                                return $"{x.Value}";
                            }
                        }));
                        writeLine($"UNK {args}");

                        break;
                }

                void writeLine(string text, int indentLevel = 1, bool includeCommand = true)
                {
                    // Write offset
                    scriptStr.Append(getFileOffsetString(cmd.Offset));
                    
                    scriptStr.Append(getIndentString());

                    // Write command
                    if (includeCommand)
                        scriptStr.Append($"{cmd.PrimaryType:00} {cmd.SecondaryType:00}");
                    else
                        scriptStr.Append(new string(' ', 5));

                    scriptStr.Append(getIndentString(indentLevel));

                    // Write text
                    scriptStr.Append(text);

                    scriptStr.AppendLine();
                }

                void writeConditionLine(string condition)
                {
                    if (cmd.ForceConditionToFalse) // ???
                    {
                        writeLine($"IF ({(cmd.InvertCondition ? "!" : String.Empty)}FALSE)");
                    }
                    else
                    {
                        // If the condition is true we run the goto
                        writeLine($"IF ({(cmd.InvertCondition ? "!" : String.Empty)}CONDITION{(condition != null ? $" {condition}" : String.Empty)})");
                    }
                }

                string getIndentString(int indentLevel = 1) => new string(' ', 4 * indentLevel);

                string getFileOffsetString(Pointer pointer) => $"0x{pointer.FileOffset - CutsceneFile.Offset.FileOffset:X4}";
            }

            ScriptText = scriptStr.ToString();
        }
    }
}