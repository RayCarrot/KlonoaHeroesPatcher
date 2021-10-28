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
            IEnumerable<TextItemViewModel> textCommands = CutsceneFile.Commands.
                Select(x => x.TextCommands).
                Where(x => x != null).
                Distinct().
                Select(x => new TextItemViewModel(this, x, getRelativeOffset(x.Offset)));

            IEnumerable<TextItemViewModel> textArrayCommands = CutsceneFile.Commands.
                Select(x => x.TextCommandsArray).
                Where(x => x != null).
                Distinct().
                SelectMany(x => x.Files.
                    Select((f, i) => new { File = f, Index = i}).
                    Where(f => f.File != null).
                    Select(f => new TextItemViewModel(this, f.File, $"{getRelativeOffset(f.File.Offset)}[{f.Index}]")));

            return textCommands.Concat(textArrayCommands);

            string getRelativeOffset(Pointer offset) => $"0x{offset.FileOffset - CutsceneFile.Offset.FileOffset:X4}";
        }

        protected override void RelocateTextCommands()
        {
            Pointer offset = CutsceneFile.Commands.Last().Offset + CutsceneFile.Commands.Last().Size;
            Dictionary<object, long> newTextOffsets = new Dictionary<object, long>();

            align();

            // Re-locate text
            foreach (CutsceneCommand cmd in CutsceneFile.Commands.Where(x => x.Type == CutsceneCommand.CommandType.SetText).Reverse())
            {
                if (newTextOffsets.ContainsKey(cmd.TextCommands))
                {
                    cmd.TextOffsetOffset = (short)((newTextOffsets[cmd.TextCommands] - cmd.Offset.FileOffset) / 4);
                    cmd.TextOffset = 1;
                }
                else
                {
                    cmd.TextOffsetOffset = (short)((offset.FileOffset - cmd.Offset.FileOffset) / 4);
                    cmd.TextOffset = 1;

                    newTextOffsets[cmd.TextCommands] = offset.FileOffset;
                    
                    cmd.TextCommands.RecalculateSize();
                    offset += 4 + cmd.TextCommands.Size;
                    align();
                }
            }

            // Re-locate text arrays
            foreach (CutsceneCommand cmd in CutsceneFile.Commands.Where(x => x.Type == CutsceneCommand.CommandType.SetTextMulti).Reverse())
            {
                if (newTextOffsets.ContainsKey(cmd.TextCommandsArray))
                {
                    cmd.TextOffsetOffset = (short)((newTextOffsets[cmd.TextCommandsArray] - cmd.Offset.FileOffset) / 4);
                    cmd.TextOffset = 1;
                }
                else
                {
                    cmd.TextOffsetOffset = (short)((offset.FileOffset - cmd.Offset.FileOffset) / 4);
                    cmd.TextOffset = 1;

                    newTextOffsets[cmd.TextCommandsArray] = offset.FileOffset;

                    offset += 4; // Text offset
                    var anchor = offset;
                    offset += cmd.TextCommandsArray.OffsetTable.Size;

                    for (int i = 0; i < cmd.TextCommandsArray.Files.Length; i++)
                    {
                        TextCommands file = cmd.TextCommandsArray.Files[i];

                        if (file == null)
                            continue;

                        cmd.TextCommandsArray.OffsetTable.FilePointers[i] = offset.SetAnchor(anchor);

                        file.RecalculateSize();
                        offset += file.Size;
                        align();
                    }

                    cmd.TextCommandsArray.CalculateFileEndPointers();

                    align();
                }
            }

            void align()
            {
                if (offset.FileOffset % 4 != 0)
                    offset += 4 - offset.FileOffset % 4;
            }

            CutsceneFile.Pre_FileSize = offset.FileOffset - CutsceneFile.Offset.FileOffset;

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
                        writeLine($"FILE REF {cmd.FileIndex_0}-{cmd.FileIndex_1}-{cmd.FileIndex_2} {cmd.FileIndexRelated_3}");
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