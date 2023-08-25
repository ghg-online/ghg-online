/*  
 *  Namespace   :   client.Gui
 *  Filename    :   InputDialog.cs
 *  Class       :   InputDialog
 *  
 *  Creator     :   Nictheboy
 *  Create at   :   2023/08/22
 *  Last Modify :   2023/08/22
 *  
 */

using NStack;
using Terminal.Gui;

namespace client.Gui
{
    /// <summary>
    /// A dialog that prompts the user for a string input.
    /// </summary>
    public class InputDialog : Dialog
    {
        /// <summary>
        /// The text entered by the user.
        /// </summary>
        public ustring Value { get { return textField.Text; } }

        /// <summary>
        /// Whether the user confirmed the dialog.
        /// </summary>
        public bool Confirmed = false;

        /// <summary>
        /// Creates a new InputDialog without an info label.
        /// </summary>
        /// <param name="Title"></param>
        /// <param name="Prompt">Something like "Username: "</param>
        /// <param name="DefaultText">The default text shown in the text field</param>
        public InputDialog(ustring Title, ustring Prompt, ustring DefaultText)
            => Init(Title, "", Prompt, DefaultText);

        /// <summary>
        /// Creates a new InputDialog with an info label.
        /// </summary>
        /// <param name="Title"></param>
        /// <param name="Info">Some text shown before <c>Prompt</c> and <c>Textfield</c></param>
        /// <param name="Prompt">Something like "Username: "</param>
        /// <param name="DefaultText">The default text shown in the text field</param>
        public InputDialog(ustring Title, ustring Info, ustring Prompt, ustring DefaultText)
            => Init(Title, Info, Prompt, DefaultText);

        /// <summary>
        /// Ask user for input a string.
        /// </summary>
        /// <param name="Title"></param>
        /// <param name="Prompt">Something like "Username: "</param>
        /// <param name="DefaultText">The default text shown in the text field</param>
        /// <returns>Whether user confirms the input</returns>
        public static bool Query(ustring Title, ustring Prompt, ustring DefaultText, out ustring Input)
        {
            InputDialog dialog = new(Title, Prompt, DefaultText);
            Application.Run(dialog);
            Input = dialog.Value;
            return dialog.Confirmed;
        }

        /// <summary>
        /// Ask user for input a string.
        /// </summary>
        /// <param name="Title"></param>
        /// <param name="Info">Some text shown before <c>Prompt</c> and <c>Textfield</c></param>
        /// <param name="Prompt">Something like "Username: "</param>
        /// <param name="DefaultText">The default text shown in the text field</param>
        /// <returns>Whether user confirms the input</returns>
        public static bool Query(ustring Title, ustring Info, ustring Prompt, ustring DefaultText, out ustring Input)
        {
            InputDialog dialog = new(Title, Info, Prompt, DefaultText);
            Application.Run(dialog);
            Input = dialog.Value;
            return dialog.Confirmed;
        }

        void OnOkButtonClicked()
        {
            Confirmed = true;
            Application.RequestStop();
        }

        void OnCancelButtonClicked()
        {
            Confirmed = false;
            Application.RequestStop();
        }

        void OnKeyPress(KeyEventEventArgs obj)
        {
            if (obj.KeyEvent.Key == Key.Esc)
            {
                Application.RequestStop();
            }
        }

        private void Init(ustring title, ustring info, ustring prompt, ustring defaultText)
        {
            int widthSize = defaultText.Length > 40 ? defaultText.Length + 10 : 50;
            int infoHeight = string.IsNullOrEmpty(info.ToString()) ? 0 : TextFormatter.MaxLines(info, widthSize);
            label.Y += infoHeight;
            textField.Y += infoHeight;
            cancelButton.Y += infoHeight;
            okButton.Y += infoHeight;

            Title = title;
            Width = widthSize;
            Height = 4 + infoHeight;

            infoLabel.Text = info;
            label.Text = prompt;
            textField.X = Pos.Right(label);
            textField.Text = defaultText;
            textField.Width = Dim.Fill();
            textField.SelectAll();
            textField.SetFocus();
            okButton.X = widthSize - 10;
            cancelButton.X = widthSize - 20;

            this.Add(infoLabel, label, textField, cancelButton, okButton);
            okButton.Clicked += OnOkButtonClicked;
            cancelButton.Clicked += OnCancelButtonClicked;
            KeyPress += OnKeyPress;
        }

        readonly Label infoLabel = new()
        {
            LayoutStyle = LayoutStyle.Computed,
            TextAlignment = TextAlignment.Centered,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            AutoSize = false
        };
        readonly Label label = new() { Y = 0 };
        readonly TextField textField = new() { Y = 0 };
        readonly Button cancelButton = new() { Y = 1, Text = "Cancel", };
        readonly Button okButton = new() { Y = 1, Text = "Ok", IsDefault = true, };
    }
}
