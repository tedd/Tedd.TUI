using System.Collections.Generic;
using Tedd.TUI.CodeColoring;

namespace Tedd.TUI.CodeColoring.Languages;

public class Asm6502Language : ILanguage
{
    public string Id => "asm6502";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@";.*"));
        grammar.Add("directive", new Pattern(@"\.\w+(?= )", alias: "property"));
        grammar.Add("string", new Pattern(@"([""'`])(?:\\.|(?!\1)[^\\\r\n])*\1"));
        grammar.Add("op-code", new Pattern(@"\b(?:ADC|AND|ASL|BCC|BCS|BEQ|BIT|BMI|BNE|BPL|BRK|BVC|BVS|CLC|CLD|CLI|CLV|CMP|CPX|CPY|DEC|DEX|DEY|EOR|INC|INX|INY|JMP|JSR|LDA|LDX|LDY|LSR|NOP|ORA|PHA|PHP|PLA|PLP|ROL|ROR|RTI|RTS|SBC|SEC|SED|SEI|STA|STX|STY|TAX|TAY|TSX|TXA|TXS|TYA|adc|and|asl|bcc|bcs|beq|bit|bmi|bne|bpl|brk|bvc|bvs|clc|cld|cli|clv|cmp|cpx|cpy|dec|dex|dey|eor|inc|inx|iny|jmp|jsr|lda|ldx|ldy|lsr|nop|ora|pha|php|pla|plp|rol|ror|rti|rts|sbc|sec|sed|sei|sta|stx|sty|tax|tay|tsx|txa|txs|tya)\b", alias: "keyword"));
        grammar.Add("hex-number", new Pattern(@"#?\$[\da-f]{1,4}\b", regexOptions: "i", alias: "number"));
        grammar.Add("binary-number", new Pattern(@"#?%[01]+\b", alias: "number"));
        grammar.Add("decimal-number", new Pattern(@"#?\b\d+\b", alias: "number"));
        grammar.Add("register", new Pattern(@"\b[xya]\b", regexOptions: "i", alias: "variable"));
        grammar.Add("punctuation", new Pattern(@"[(),:]"));
        return grammar;
    }
}
