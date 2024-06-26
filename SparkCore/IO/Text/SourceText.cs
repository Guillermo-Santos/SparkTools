﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SparkCore.IO.Text;

public sealed class SourceText
{

    private readonly string _text;
    private SourceText(string text, string fileName)
    {
        _text = text;
        FileName = fileName;
        Lines = ParseLines(this, text);
    }
    public static SourceText From(string text, string fileName = "")
    {
        return new SourceText(text, fileName);
    }
    private List<TextLine> ParseLines(SourceText sourceText, string text)
    {
        var result = new List<TextLine>();
        var position = 0;
        var lineStart = 0;
        while (position < text.Length)
        {    
            var lineBreakWidth = GetLineBreakWidth(text, position);

            if (lineBreakWidth == 0)
            {
                position++;
            }
            else
            {
                AddLine(result, sourceText, position, lineStart, lineBreakWidth);

                position += lineBreakWidth;
                lineStart = position;
            }
        }
        if (position >= lineStart)
        {
            AddLine(result, sourceText, position, lineStart, 0);
        }

        return result;
    }
    public List<TextLine> Lines
    {
        get;
    }
    public char this[int index] => _text[index];
    public int Length => _text.Length;
    public string FileName
    {
        get;
    }
    public int GetLineIndex(int position)
    {
        var lower = 0;
        var upper = Lines.Count - 1;
        while (lower <= upper)
        {
            var index = lower + (upper - lower) / 2;
            var start = Lines[index].Start;

            if (position == start)
            {
                return index;
            }

            if (start > position)
            {
                upper = index - 1;
            }
            else
            {
                lower = index + 1;
            }
        }
        return lower - 1;
    }

    private static void AddLine(List<TextLine> result, SourceText sourceText, int position, int lineStart, int lineBreakWidth)
    {
        var lineLenght = position - lineStart;
        var lineLengthIncludingLinewBreak = lineLenght + lineBreakWidth;
        var line = new TextLine(sourceText, lineStart, lineLenght, lineLengthIncludingLinewBreak);
        result.Add(line);
    }


    private int GetLineBreakWidth(string text, int position)
    {
        var c = text[position];
        var l = position + 1 >= text.Length ? '\0' : text[position + 1];
        if (c == '\r' && l == '\n')
        {
            return 2;
        }
        else if (c == '\r' || c == '\n')
        {
            return 1;
        }

        return 0;
    }

    public override string ToString() => _text;
    public string ToString(int start, int length) => _text.Substring(start, length);
    public string ToString(TextSpan span) => _text.Substring(span.Start, span.Length);

}

