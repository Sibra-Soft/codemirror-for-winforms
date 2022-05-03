Imports System.Windows.Forms
Imports System.Net
Imports CefSharp.WinForms
Imports CefSharp
Imports CefSharp.WebBrowserExtensions
Imports System.Text

Namespace CodeMirror
    Public Class CodeMirror
        Public Bookmarks As New List(Of Models.CodeMirrorBookmarkModel)
        Public ValidationErrors As New List(Of Models.ValidationResultModel)

        Private WebControl As ChromiumWebBrowser
        Private WithEvents InternalTimer As New Timer

        Public Property ValidateHTMLCode As Boolean = False
        Public Property IsDirty As Boolean

        Public Event SaveDocument()
        Public Event BookmarkAdded()
        Public Event BookmarkDeleted()
        Public Event DocumentSaved()
        Public Event DocumentLoaded()
        Private Function TypeFromEnum(type As DocTye) As String

            Select Case type
                Case DocTye.CssStylesheet : Return "css"
                Case DocTye.Html : Return "html"
                Case DocTye.Javascript : Return "javascript"
                Case DocTye.Php : Return "php"
                Case DocTye.Sql : Return "sql"
                Case DocTye.Markdown : Return "markdown"
            End Select

            Return "html"
        End Function
        Public Function CreateCodeMirrorControl() As Control
            Dim settings = New BrowserSettings With {
                .DefaultEncoding = "utf-8"
            }

            Dim browser As New ChromiumWebBrowser("") With {
                .Top = 0,
                .Left = 0,
                .Dock = DockStyle.Fill,
                .Visible = True,
                .Name = "Webbrowser",
                .BrowserSettings = settings
            }

            WebControl = browser

            Return browser
        End Function
        Public Function LineNumber() As Integer
            Return Convert.ToInt32(InvokeEditorFunction("editor.getCursor().line")) + 1
        End Function
        Public Sub SelectNextBookmark()

        End Sub
        Public Sub SelectPreviousBookmark()

        End Sub
        Public Sub Gotoline(lineNumber As Integer)
            InvokeEditorFunction(String.Format("JumpToLine({0})", lineNumber))
        End Sub
        Public Sub Find(word As String, Optional matchCase As Boolean = False)
            WebControl.Find(word, True, matchCase, True)
        End Sub
        Public Sub AddBookmark(Optional line As Integer = Nothing)
            If line = Nothing Then
                InvokeEditorFunction("AddBookmark()")
                Bookmarks.Add(New Models.CodeMirrorBookmarkModel With {
                    .Line = LineNumber(),
                    .Timestamp = Date.Now()
                })
            Else
                InvokeEditorFunction(String.Format("AddBookmark({0})", line))
            End If

            RaiseEvent BookmarkAdded()
        End Sub
        Public Sub RemoveBookmark(Optional line As Integer = Nothing)
            If line = Nothing Then
                InvokeEditorFunction("RemoveBookmark()")
            Else
                InvokeEditorFunction(String.Format("RemoveBookmark({0})", line))
            End If

            RaiseEvent BookmarkDeleted()
        End Sub
        Public Sub ClearBookmarks()
            InvokeEditorFunction("ClearBookmarks()")
            RaiseEvent BookmarkDeleted()
        End Sub
        Public Sub Delete()
            SendKey(CefEventFlags.None, Keys.Delete)
        End Sub
        Public Sub SelectAll()
            SendKey(CefEventFlags.ControlDown, Keys.A)
        End Sub
        Public Sub Copy()
            SendKey(CefEventFlags.ControlDown, Keys.C)
        End Sub
        Public Sub Undo()
            SendKey(CefEventFlags.ControlDown, Keys.Z)
        End Sub
        Public Sub Redo()
            SendKey(CefEventFlags.ControlDown, Keys.Y)
        End Sub
        Public Sub Cut()
            SendKey(CefEventFlags.ControlDown, Keys.X)
        End Sub
        Public Sub Paste()
            SendKey(CefEventFlags.ControlDown, Keys.V)
        End Sub
        Public Sub Save()
            InvokeEditorFunction("IsSaved()")

            RaiseEvent SaveDocument()
        End Sub
        Private Sub SendKey(modifier As CefEventFlags, key As Keys)
            WebControl.GetBrowser().GetHost().SendKeyEvent(New KeyEvent With {
                .WindowsKeyCode = key,
                .Type = KeyEventType.KeyDown,
                .FocusOnEditableField = True,
                .IsSystemKey = False,
                .Modifiers = modifier
            })
        End Sub
        Public Sub Print()
            WebControl.GetBrowserHost().Print()
        End Sub
        Public Function Content() As String
            Return InvokeEditorFunction("GetContent()")
        End Function
        Public Sub LoadDocument(Type As DocTye, Optional content As String = "")
            Dim docType As String = TypeFromEnum(Type)
            Dim outputBuilder As New StringBuilder()
            Dim htmlContent As String = WebUtility.HtmlEncode(content)

            outputBuilder.Append(My.Resources.codemirror)
            outputBuilder.Replace("{codemirror_textbox}", "<textarea id=" & Chr(34) & "code" & Chr(34) & " name=" & Chr(34) & "code" & Chr(34) & " data-type=" & Chr(34) & docType & Chr(34) & " >" & htmlContent & "</textarea>")

            WebControl.LoadHtml(outputBuilder.ToString())

            InternalTimer.Enabled = True
            InternalTimer.Interval = 10

            RaiseEvent DocumentLoaded()
        End Sub
        Private Function InvokeEditorFunction(name As String) As String
            If WebControl.CanExecuteJavascriptInMainFrame Then
                Dim task = WebControl.EvaluateScriptAsync(name)
                task.Wait()

                Dim response As JavascriptResponse = task.Result

                If response.Success Then
                    If response.Result Is Nothing Then
                        Return ""
                    Else
                        Return response.Result.ToString()
                    End If
                Else
                    Return response.Message
                End If
            End If

            Return ""
        End Function

        Private Sub InternalTimer_Tick(sender As Object, e As EventArgs) Handles InternalTimer.Tick
            IsDirty = InvokeEditorFunction("IsChanged()") = "True"

            If InvokeEditorFunction("MustBeSaved()") = "True" Then Save()
        End Sub
    End Class
End Namespace