
using System;
using System.Collections.Generic;
[Serializable]
public class MacroRecord
{
    static List<MacroRecord>  macroRecords = new List<MacroRecord>();
    private string FilePath;
    private int LineNum;
    private string Content;

    public MacroRecord(string filePath, int lineNum, string content)
    {
        FilePath=filePath;
        LineNum=lineNum;
        Content=content;
    }
    
}

