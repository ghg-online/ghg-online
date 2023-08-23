using NStack;
using Terminal.Gui;

namespace client.Gui
{
    public class MultiInputDialog : Dialog
    {
        public bool Ok { get; private set; } = false;
        public ustring[]? Values { get; private set; } = null;

        public static bool Query(ustring title, ustring message, ustring[] labels, ustring?[]? values, out ustring[]? result)
        {
            var dialog = new MultiInputDialog(title, message, labels, values);
            Application.Run(dialog);
            result = dialog.Values;
            return dialog.Ok;
        }

        public static bool Query(ustring title, ustring message, ustring[] labels, ustring?[]? values, int inputWidth, out ustring[]? result)
        {
            var dialog = new MultiInputDialog(title, message, labels, values, inputWidth);
            Application.Run(dialog);
            result = dialog.Values;
            return dialog.Ok;
        }

        public MultiInputDialog(ustring title, ustring message, ustring[] labels, ustring?[]? values)
            => Init(title, message, labels, values, 30);

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
