using System;
using System.Collections.Generic;

namespace KlonoaHeroesPatcher;

public class AppConfig
{
    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="fontTable">The font table, indicating the string each font index represents</param>
    /// <param name="textStringComparison">The string comparison to use for text</param>
    /// <param name="serializerLogPath">An optional serializer log path</param>
    /// <param name="useFileLogging">Indicates if file logging is enabled</param>
    /// <param name="logLevel">Indicates the log level to use for the logging</param>
    /// <param name="romEndPointer">Indicates where to start appending data to the ROM</param>
    public AppConfig(Dictionary<int, string[]> fontTable, StringComparison? textStringComparison, string serializerLogPath, bool? useFileLogging, string logLevel, uint? romEndPointer)
    {
        FontTable = fontTable ?? new Dictionary<int, string[]>()
        {
            [0x000] = new string[] { "あ" },
            [0x001] = new string[] { "い" },
            [0x002] = new string[] { "う" },
            [0x003] = new string[] { "え" },
            [0x004] = new string[] { "お" },
            [0x005] = new string[] { "か" },
            [0x006] = new string[] { "き" },
            [0x007] = new string[] { "く" },
            [0x008] = new string[] { "け" },
            [0x009] = new string[] { "こ" },
            [0x00A] = new string[] { "さ" },
            [0x00B] = new string[] { "し" },
            [0x00C] = new string[] { "す" },
            [0x00D] = new string[] { "せ" },
            [0x00E] = new string[] { "そ" },
            [0x00F] = new string[] { "た" },
            [0x010] = new string[] { "ち" },
            [0x011] = new string[] { "つ" },
            [0x012] = new string[] { "て" },
            [0x013] = new string[] { "と" },
            [0x014] = new string[] { "な" },
            [0x015] = new string[] { "に" },
            [0x016] = new string[] { "ぬ" },
            [0x017] = new string[] { "ね" },
            [0x018] = new string[] { "の" },
            [0x019] = new string[] { "は" },
            [0x01A] = new string[] { "ひ" },
            [0x01B] = new string[] { "ふ" },
            [0x01C] = new string[] { "へ" },
            [0x01D] = new string[] { "ほ" },
            [0x01E] = new string[] { "ま" },
            [0x01F] = new string[] { "み" },
            [0x020] = new string[] { "む" },
            [0x021] = new string[] { "め" },
            [0x022] = new string[] { "も" },
            [0x023] = new string[] { "や" },
            [0x024] = new string[] { "ゆ" },
            [0x025] = new string[] { "よ" },
            [0x026] = new string[] { "ら" },
            [0x027] = new string[] { "り" },
            [0x028] = new string[] { "る" },
            [0x029] = new string[] { "れ" },
            [0x02A] = new string[] { "ろ" },
            [0x02B] = new string[] { "わ" },
            [0x02C] = new string[] { "を" },
            [0x02D] = new string[] { "ん" },
            [0x02E] = new string[] { "が" },
            [0x02F] = new string[] { "ぎ" },
            [0x030] = new string[] { "ぐ" },
            [0x031] = new string[] { "げ" },
            [0x032] = new string[] { "ご" },
            [0x033] = new string[] { "ざ" },
            [0x034] = new string[] { "じ" },
            [0x035] = new string[] { "ず" },
            [0x036] = new string[] { "ぜ" },
            [0x037] = new string[] { "ぞ" },
            [0x038] = new string[] { "だ" },
            [0x039] = new string[] { "ぢ" },
            [0x03A] = new string[] { "づ" },
            [0x03B] = new string[] { "で" },
            [0x03C] = new string[] { "ど" },
            [0x03D] = new string[] { "ば" },
            [0x03E] = new string[] { "び" },
            [0x03F] = new string[] { "ぶ" },
            [0x040] = new string[] { "ベ" },
            [0x041] = new string[] { "ぼ" },
            [0x042] = new string[] { "ぱ" },
            [0x043] = new string[] { "ぴ" },
            [0x044] = new string[] { "ぷ" },
            [0x045] = new string[] { "ぺ" },
            [0x046] = new string[] { "ぽ" },
            [0x047] = new string[] { "ぁ" },
            [0x048] = new string[] { "ぃ" },
            [0x049] = new string[] { "ぅ" },
            [0x04A] = new string[] { "ぇ" },
            [0x04B] = new string[] { "ぉ" },
            [0x04C] = new string[] { "っ" },
            [0x04D] = new string[] { "ゃ" },
            [0x04E] = new string[] { "ゅ" },
            [0x04F] = new string[] { "ょ" },
            [0x050] = new string[] { "ア" },
            [0x051] = new string[] { "イ" },
            [0x052] = new string[] { "ウ" },
            [0x053] = new string[] { "エ" },
            [0x054] = new string[] { "オ" },
            [0x055] = new string[] { "カ" },
            [0x056] = new string[] { "キ" },
            [0x057] = new string[] { "ク" },
            [0x058] = new string[] { "ケ" },
            [0x059] = new string[] { "コ" },
            [0x05A] = new string[] { "サ" },
            [0x05B] = new string[] { "シ" },
            [0x05C] = new string[] { "ス" },
            [0x05D] = new string[] { "セ" },
            [0x05E] = new string[] { "ソ" },
            [0x05F] = new string[] { "タ" },
            [0x060] = new string[] { "チ" },
            [0x061] = new string[] { "ツ" },
            [0x062] = new string[] { "テ" },
            [0x063] = new string[] { "ト" },
            [0x064] = new string[] { "ナ" },
            [0x065] = new string[] { "ニ" },
            [0x066] = new string[] { "ヌ" },
            [0x067] = new string[] { "ネ" },
            [0x068] = new string[] { "ノ" },
            [0x069] = new string[] { "ハ" },
            [0x06A] = new string[] { "ヒ" },
            [0x06B] = new string[] { "フ" },
            [0x06C] = new string[] { "ヘ" },
            [0x06D] = new string[] { "ホ" },
            [0x06E] = new string[] { "マ" },
            [0x06F] = new string[] { "ミ" },
            [0x070] = new string[] { "ム" },
            [0x071] = new string[] { "メ" },
            [0x072] = new string[] { "モ" },
            [0x073] = new string[] { "ヤ" },
            [0x074] = new string[] { "ユ" },
            [0x075] = new string[] { "ヨ" },
            [0x076] = new string[] { "ラ" },
            [0x077] = new string[] { "リ" },
            [0x078] = new string[] { "ル" },
            [0x079] = new string[] { "レ" },
            [0x07A] = new string[] { "ロ" },
            [0x07B] = new string[] { "ワ" },
            [0x07C] = new string[] { "ヲ" },
            [0x07D] = new string[] { "ン" },
            [0x07E] = new string[] { "ガ" },
            [0x07F] = new string[] { "ギ" },
            [0x080] = new string[] { "グ" },
            [0x081] = new string[] { "ゲ" },
            [0x082] = new string[] { "ゴ" },
            [0x083] = new string[] { "ザ" },
            [0x084] = new string[] { "ジ" },
            [0x085] = new string[] { "ズ" },
            [0x086] = new string[] { "ゼ" },
            [0x087] = new string[] { "ゾ" },
            [0x088] = new string[] { "ダ" },
            [0x089] = new string[] { "ヂ" },
            [0x08A] = new string[] { "ヅ" },
            [0x08B] = new string[] { "デ" },
            [0x08C] = new string[] { "ド" },
            [0x08D] = new string[] { "バ" },
            [0x08E] = new string[] { "ビ" },
            [0x08F] = new string[] { "ブ" },
            [0x090] = new string[] { "ベ" },
            [0x091] = new string[] { "ボ" },
            [0x092] = new string[] { "パ" },
            [0x093] = new string[] { "ピ" },
            [0x094] = new string[] { "プ" },
            [0x095] = new string[] { "ぺ" },
            [0x096] = new string[] { "ポ" },
            [0x097] = new string[] { "ァ" },
            [0x098] = new string[] { "ィ" },
            [0x099] = new string[] { "ゥ" },
            [0x09A] = new string[] { "ェ" },
            [0x09B] = new string[] { "ォ" },
            [0x09C] = new string[] { "ッ" },
            [0x09D] = new string[] { "ャ" },
            [0x09E] = new string[] { "ュ" },
            [0x09F] = new string[] { "ョ" },
            [0x0A0] = new string[] { "　", " " },
            [0x0A1] = new string[] { "ヴ" },
            [0x0A2] = new string[] { "０", "0" },
            [0x0A3] = new string[] { "１", "1" },
            [0x0A4] = new string[] { "２", "2" },
            [0x0A5] = new string[] { "３", "3" },
            [0x0A6] = new string[] { "４", "4" },
            [0x0A7] = new string[] { "５", "5" },
            [0x0A8] = new string[] { "６", "6" },
            [0x0A9] = new string[] { "７", "7" },
            [0x0AA] = new string[] { "８", "8" },
            [0x0AB] = new string[] { "９", "9" },
            [0x0AC] = new string[] { "ー", "—" },
            [0x0AD] = new string[] { "、", "," },
            [0x0AE] = new string[] { "。", "." },
            [0x0AF] = new string[] { "！", "!" },
            [0x0B0] = new string[] { "？", "?" },
            [0x0B1] = new string[] { "＋", "+" },
            [0x0B2] = new string[] { "－", "-" },
            [0x0B3] = new string[] { "×", },
            [0x0B4] = new string[] { "％", "%" },
            [0x0B5] = new string[] { "／", "/" },
            [0x0B6] = new string[] { "～", "~" },
            [0x0B7] = new string[] { "＆", "&" },
            [0x0B8] = new string[] { "「" },
            [0x0B9] = new string[] { "」" },
            [0x0BA] = new string[] { "♡" },
            [0x0BB] = new string[] { "♪" },
            [0x0BC] = new string[] { "ゐ" },
            [0x0BD] = new string[] { "ヰ" },
            [0x0BE] = new string[] { "ヱ" },
            [0x0BF] = new string[] { "＊" },
            [0x0C0] = new string[] { "ゔ" },
            [0x0C1] = new string[] { "Ａ", "A" },
            [0x0C2] = new string[] { "Ｂ", "B" },
            [0x0C3] = new string[] { "Ｃ", "C" },
            [0x0C4] = new string[] { "Ｄ", "D" },
            [0x0C5] = new string[] { "Ｅ", "E" },
            [0x0C6] = new string[] { "Ｆ", "F" },
            [0x0C7] = new string[] { "Ｇ", "G" },
            [0x0C8] = new string[] { "Ｈ", "H" },
            [0x0C9] = new string[] { "Ｉ", "I" },
            [0x0CA] = new string[] { "Ｊ", "J" },
            [0x0CB] = new string[] { "Ｋ", "K" },
            [0x0CC] = new string[] { "Ｌ", "L" },
            [0x0CD] = new string[] { "Ｍ", "M" },
            [0x0CE] = new string[] { "Ｎ", "N" },
            [0x0CF] = new string[] { "Ｏ", "O" },
            [0x0D0] = new string[] { "Ｐ", "P" },
            [0x0D1] = new string[] { "Ｑ", "Q" },
            [0x0D2] = new string[] { "Ｒ", "R" },
            [0x0D3] = new string[] { "Ｓ", "S" },
            [0x0D4] = new string[] { "Ｔ", "T" },
            [0x0D5] = new string[] { "Ｕ", "U" },
            [0x0D6] = new string[] { "Ｖ", "V" },
            [0x0D7] = new string[] { "Ｗ", "W" },
            [0x0D8] = new string[] { "Ｘ", "X" },
            [0x0D9] = new string[] { "Ｙ", "Y" },
            [0x0DA] = new string[] { "Ｚ", "Z" },
            [0x0DB] = new string[] { "：", ":" },
            [0x0DC] = new string[] { "…" },
            [0x0DD] = new string[] { "＜", "<" },
            [0x0DE] = new string[] { "＞", ">" },
            [0x0DF] = new string[] { "．" },
            [0x0E0] = new string[] { "（", "(" },
            [0x0E1] = new string[] { "）", ")" },
            [0x0E2] = new string[] { "・" },
            [0x0E3] = new string[] { "『" },
            [0x0E4] = new string[] { "』" },
            [0x0E5] = new string[] { "【" },
            [0x0E6] = new string[] { "】" },
            [0x0E7] = new string[] { "T'" },
            [0x0E8] = new string[] { "'L" },
            [0x0E9] = new string[] { "㈱" },
            [0x0EA] = new string[] { "㈱" },
            [0x0EB] = new string[] { "＇" },
            [0x0EC] = new string[] { "＂" },
            //[0x0ED] = "[0ED]",
            //[0x0EE] = "[0EE]",
            //[0x0EF] = "[0EF]",
            //[0x0F0] = "[0F0]",
            //[0x0F1] = "[0F1]",
            //[0x0F2] = "[0F2]",
            //[0x0F3] = "[0F3]",
            //[0x0F4] = "[0F4]",
            //[0x0F5] = "[0F5]",
            //[0x0F6] = "[0F6]",
            //[0x0F7] = "[0F7]",
            //[0x0F8] = "[0F8]",
            //[0x0F9] = "[0F9]",
            //[0x0FA] = "[0FA]",
            //[0x0FB] = "[0FB]",
            //[0x0FC] = "[0FC]",
            //[0x0FD] = "[0FD]",
            //[0x0FE] = "[0FE]",
            //[0x0FF] = "[0FF]",
            [0x100] = new string[] { "月" },
            [0x101] = new string[] { "夢" },
            [0x102] = new string[] { "日" },
            [0x103] = new string[] { "晴" },
            [0x104] = new string[] { "春" },
            [0x105] = new string[] { "年" },
            [0x106] = new string[] { "幸" },
            [0x107] = new string[] { "古" },
            [0x108] = new string[] { "信" },
            [0x109] = new string[] { "夕" },
            [0x10A] = new string[] { "村" },
            [0x10B] = new string[] { "寺" },
            [0x10C] = new string[] { "天" },
            [0x10D] = new string[] { "空" },
            [0x10E] = new string[] { "院" },
            [0x10F] = new string[] { "今" },
            [0x110] = new string[] { "丘" },
            [0x111] = new string[] { "安" },
            [0x112] = new string[] { "心" },
            [0x113] = new string[] { "立" },
            [0x114] = new string[] { "入" },
            [0x115] = new string[] { "金" },
            [0x116] = new string[] { "色" },
            [0x117] = new string[] { "光" },
            [0x118] = new string[] { "花" },
            [0x119] = new string[] { "実" },
            [0x11A] = new string[] { "力" },
            [0x11B] = new string[] { "強" },
            [0x11C] = new string[] { "方" },
            [0x11D] = new string[] { "法" },
            [0x11E] = new string[] { "目" },
            [0x11F] = new string[] { "前" },
            [0x120] = new string[] { "気" },
            [0x121] = new string[] { "合" },
            [0x122] = new string[] { "場" },
            [0x123] = new string[] { "負" },
            [0x124] = new string[] { "名" },
            [0x125] = new string[] { "明" },
            [0x126] = new string[] { "朝" },
            [0x127] = new string[] { "決" },
            [0x128] = new string[] { "先" },
            [0x129] = new string[] { "大" },
            [0x12A] = new string[] { "欠" },
            [0x12B] = new string[] { "点" },
            [0x12C] = new string[] { "面" },
            [0x12D] = new string[] { "岩" },
            [0x12E] = new string[] { "人" },
            [0x12F] = new string[] { "正" },
            [0x130] = new string[] { "所" },
            [0x131] = new string[] { "上" },
            [0x132] = new string[] { "世" },
            [0x133] = new string[] { "界" },
            [0x134] = new string[] { "原" },
            [0x135] = new string[] { "固" },
            [0x136] = new string[] { "小" },
            [0x137] = new string[] { "予" },
            [0x138] = new string[] { "想" },
            [0x139] = new string[] { "行" },
            [0x13A] = new string[] { "不" },
            [0x13B] = new string[] { "近" },
            [0x13C] = new string[] { "中" },
            [0x13D] = new string[] { "口" },
            [0x13E] = new string[] { "者" },
            [0x13F] = new string[] { "穴" },
            [0x140] = new string[] { "外" },
            [0x141] = new string[] { "悪" },
            [0x142] = new string[] { "送" },
            [0x143] = new string[] { "追" },
            [0x144] = new string[] { "本" },
            [0x145] = new string[] { "々" },
            [0x146] = new string[] { "消" },
            [0x147] = new string[] { "間" },
            [0x148] = new string[] { "赤" },
            [0x149] = new string[] { "水" },
            [0x14A] = new string[] { "初" },
            [0x14B] = new string[] { "会" },
            [0x14C] = new string[] { "少" },
            [0x14D] = new string[] { "空" },
            [0x14E] = new string[] { "足" },
            [0x14F] = new string[] { "元" },
            [0x150] = new string[] { "宇" },
            [0x151] = new string[] { "宙" },
            [0x152] = new string[] { "作" },
            [0x153] = new string[] { "伝" },
            [0x154] = new string[] { "説" },
            [0x155] = new string[] { "臣" },
            [0x156] = new string[] { "化" },
            [0x157] = new string[] { "自" },
            [0x158] = new string[] { "信" },
            [0x159] = new string[] { "差" },
            [0x15A] = new string[] { "一" },
            [0x15B] = new string[] { "歩" },
            [0x15C] = new string[] { "工" },
            [0x15D] = new string[] { "業" },
            [0x15E] = new string[] { "都" },
            [0x15F] = new string[] { "市" },
            [0x160] = new string[] { "先" },
            [0x161] = new string[] { "生" },
            [0x162] = new string[] { "見" },
            [0x163] = new string[] { "三" },
            [0x164] = new string[] { "二" },
            [0x165] = new string[] { "打" },
            [0x166] = new string[] { "発" },
            [0x167] = new string[] { "急" },
            [0x168] = new string[] { "火" },
            [0x169] = new string[] { "全" },
            [0x16A] = new string[] { "国" },
            [0x16B] = new string[] { "出" },
            [0x16C] = new string[] { "回" },
            [0x16D] = new string[] { "倍" },
            [0x16E] = new string[] { "木" },
            [0x16F] = new string[] { "代" },
            [0x170] = new string[] { "的" },
            [0x171] = new string[] { "令" },
            [0x172] = new string[] { "当" },
            [0x173] = new string[] { "了" },
            [0x174] = new string[] { "時" },
            [0x175] = new string[] { "話" },
            [0x176] = new string[] { "高" },
            [0x177] = new string[] { "声" },
            [0x178] = new string[] { "男" },
            [0x179] = new string[] { "川" },
            [0x17A] = new string[] { "分" },
            //[0x17B] = "[17B]",
            //[0x17C] = "[17C]",
            //[0x17D] = "[17D]",
            //[0x17E] = "[17E]",
            //[0x17F] = "[17F]",
        };
        TextStringComparison = textStringComparison ?? StringComparison.InvariantCultureIgnoreCase;
        SerializerLogPath = serializerLogPath;
        UseFileLogging = useFileLogging ?? true;
        LogLevel = logLevel;
        ROMEndPointer = romEndPointer ?? 0x08FD5440;
    }

    /// <summary>
    /// The font table, indicating the string each font index represents
    /// </summary>
    public Dictionary<int, string[]> FontTable { get; }

    /// <summary>
    /// The string comparison to use for text
    /// </summary>
    public StringComparison TextStringComparison { get; }

    /// <summary>
    /// An optional serializer log path
    /// </summary>
    public string SerializerLogPath { get; }

    /// <summary>
    /// Indicates if file logging is enabled
    /// </summary>
    public bool UseFileLogging { get; }

    /// <summary>
    /// Indicates the log level to use for the logging
    /// </summary>
    public string LogLevel { get; }

    /// <summary>
    /// Indicates where to start appending data to the ROM
    /// </summary>
    public uint ROMEndPointer { get; }

    /// <summary>
    /// Gets a default configuration
    /// </summary>
    public static AppConfig Default => new AppConfig(null, null, null, null, null, null);
}