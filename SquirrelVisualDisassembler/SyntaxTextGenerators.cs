// using System;
// using System.Text.RegularExpressions;
// using Avalonia.Input;
// using Avalonia.Media;
// using AvaloniaEdit.Document;
// using AvaloniaEdit.Rendering;
// using AvaloniaEdit.Text;
//
// namespace SquirrelVisualDisassembler {
//     public class ElementGenerator : VisualLineElementGenerator {
//         private Regex regex = new Regex(@"ip \+= (-?[0-9]+) \[[0-9]+\]$");
//
//         private Match Find(int startOffset) {
//             int endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
//             TextDocument document = CurrentContext.Document;
//             string relevantText = document.GetText(startOffset, endOffset - startOffset);
//             return regex.Match(relevantText);
//         }
//
//         public override int GetFirstInterestedOffset(int startOffset) {
//             Match m = Find(startOffset);
//             return m.Success ? (startOffset + m.Index) : -1;
//         }
//
//         public override VisualLineElement ConstructElement(int offset) {
//             Match m = Find(offset);
//             // check whether there's a match exactly at offset
//             if (m.Success && m.Index == 0) {
//                 DocumentLine offsetLine = CurrentContext.Document.GetLineByOffset(offset);
//                 JumpVisualLineText line = new JumpVisualLineText(CurrentContext.VisualLine, m.Length) {
//                     line = offsetLine.LineNumber + int.Parse(m.Groups[1].Captures[0].Value)
//                 };
//                 Console.WriteLine($"JUMP MATCHED {line.line}");
//                 return line;
//             }
//
//             return new Visual;
//         }
//     }
//
//     public abstract class SpecialVisualLineText : VisualLineText {
//         internal int line;
//         private IBrush foregroundColor;
//         private bool hasUnderline;
//
//         protected SpecialVisualLineText(VisualLine parentVisualLine, int length, IBrush foreground, bool underline) : base(parentVisualLine, length) {
//             foregroundColor = foreground;
//             hasUnderline = underline;
//         }
//         
//         public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context) {
//             TextRunProperties.ForegroundBrush = foregroundColor;
//             TextRunProperties.Underline = hasUnderline;
//             return base.CreateTextRun(startVisualColumn, context);
//         }
//     }
//
//     public class JumpVisualLineText : SpecialVisualLineText {
//         internal int line;
//         public JumpVisualLineText(VisualLine parentVisualLine, int length) : base(parentVisualLine, length, Brushes.CornflowerBlue, true) { }
//
//         protected override void OnQueryCursor(PointerEventArgs e) {
//             e.Handled = true;
//             if (e.Source is InputElement source) {
//                 Console.WriteLine($"checked {source.IsPointerOver}");
//                 source.Cursor = new Cursor(StandardCursorType.Hand);
//             }
//         }
//
//         protected override void OnPointerPressed(PointerPressedEventArgs e) {
//             e.Handled = true;
//             if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) {
//                 MainWindowViewModel.Instance.DisassemblyJumpClick(line);
//             }
//         }
//     }
// }