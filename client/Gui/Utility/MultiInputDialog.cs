/*  
 *  Namespace   :   client.Gui
 *  Filename    :   MultiInputDialog.cs
 *  Class       :   MultiInputDialog
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
    /// A dialog that allows the user to input multiple string values.
    /// </summary>
    public class MultiInputDialog : Dialog
    {
        /// <summary>
        /// Whether the user clicked the Ok button to confirm the input.
        /// </summary>
        public bool Ok { get; private set; } = false;

        /// <summary>
        /// The value that the user input.
        /// </summary>
        /// <remarks>
        /// <para>If the user does not confirm input, the value is null.</para>
        /// <para>If the user confirms input, the value is a string array.</para>
        /// </remarks>
        public ustring[]? Values { get; private set; } = null;

        /// <summary>
        /// Ask the user to input multiple string values.
        /// </summary>
        /// <param name="title">The title of the dialog</param>
        /// <param name="message">Something that is shown above all input fields</param>
        /// <param name="labels">The prompt shown before input fields</param>
        /// <param name="values">The default values of the input fields</param>
        /// <param name="result">The result that user entered, null if user does not confirm</param>
        /// <returns></returns>
        public static bool Query(ustring title, ustring message, ustring[] labels, ustring?[]? values, out ustring[]? result)
        {
            var dialog = new MultiInputDialog(title, message, labels, values);
            Application.Run(dialog);
            result = dialog.Values;
            return dialog.Ok;
        }

        /// <summary>
        /// Ask the user to input multiple string values.
        /// </summary>
        /// <param name="title">The title of the dialog</param>
        /// <param name="message">Something that is shown above all input fields</param>
        /// <param name="labels">The prompt shown before input fields</param>
        /// <param name="values">The default values of the input fields</param>
        /// <param name="inputWidth">The width of the input field</param>
        /// <param name="result">The result that user entered, null if user does not confirm</param>
        /// <returns></returns>
        /// <summary>
        public static bool Query(ustring title, ustring message, ustring[] labels, ustring?[]? values, int inputWidth, out ustring[]? result)
        {
            var dialog = new MultiInputDialog(title, message, labels, values, inputWidth);
            Application.Run(dialog);
            result = dialog.Values;
            return dialog.Ok;
        }

        /// <summary>
        /// Create a dialog that allows the user to input multiple string values.
        /// </summary>
        /// <param name="title">The title of the dialog</param>
        /// <param name="message">Something that is shown above all input fields</param>
        /// <param name="labels">The prompt shown before input fields</param>
        /// <param name="values">The default values of the input fields</param>
        public MultiInputDialog(ustring title, ustring message, ustring[] labels, ustring?[]? values)
            => Init(title, message, labels, values, 30);


        /// <summary>
        /// Create a dialog that allows the user to input multiple string values.
        /// </summary>
        /// <param name="title">The title of the dialog</param>
        /// <param name="message">Something that is shown above all input fields</param>
        /// <param name="labels">The prompt shown before input fields</param>
        /// <param name="values">The default values of the input fields</param>
        /// <param name="inputWidth">The width of the input field</param>
        public MultiInputDialog(ustring title, ustring message, ustring[] labels, ustring?[]? values, int inputWidth)
            => Init(title, message, labels, values, inputWidth);


        private void Init(ustring title, ustring message, ustring[] labels, ustring?[]? values, int inputWidth)
        {
            Title = title;
            // #--LABEL-[------------INPUT-----------]--# 
            int maxLabelLength = labels.Select(label => label.Length).Max();
            int width = maxLabelLength + inputWidth + 10;
            Width = width;

            int y = 1;
            var messageLabel = new Label(message)
            {
                X = Pos.Center(),
                Y = y++,
                TextAlignment = TextAlignment.Centered,
                Width = Dim.Fill(3),
            };
            Add(messageLabel);
            y += messageLabel.TextFormatter.Lines.Count;

            var inputFields = new TextField[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                var label = new Label(labels[i])
                {
                    X = 3,
                    Y = y,
                    Width = maxLabelLength,
                    Height = 1,
                };
                Add(label);

                var inputField = new TextField()
                {
                    X = 2 + maxLabelLength + 1 + 1,
                    Y = y,
                    Width = inputWidth,
                    Height = 1,
                };
                if(values is not null)
                    if (values[i]is not null)
                        inputField.Text = values[i];
                Add(inputField);
                inputFields[i] = inputField;

                y += 2;
            }

            var cancelButton = new Button("Cancel")
            {
                X = width - 26,
                Y = y,
            };
            Add(cancelButton);

            var okButton = new Button("  Ok  ")
            {
                X = width - 16,
                Y = y,
                Width = 10,
            };
            Add(okButton);

            okButton.Clicked += () =>
            {
                this.Ok = true;
                this.Values = inputFields.Select(inputField => inputField.Text).ToArray();
                Application.RequestStop();
            };

            cancelButton.Clicked += () =>
            {
                this.Ok = false;
                Application.RequestStop();
            };

            Height = y + 4;
        }
    }
}
