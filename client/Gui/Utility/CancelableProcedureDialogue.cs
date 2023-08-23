/*  
 *  Namespace   :   client.Gui
 *  Filename    :   CancelableProcedureDialogue.cs
 *  Class       :   CancelableProcedureDialogue
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
    /// A dialog that can be used to display information about a procedure that can be canceled.
    /// </summary>
    /// <remarks>
    /// You need to assign a function that will be called when the cancel button is clicked.
    /// </remarks>
    public class CancelableProcedureDialogue : Dialog
    {
        /// <summary>
        /// The default value is true. If the procedure is canceled, this value will be set to false.
        /// </summary>
        public bool IsCanceled { get; private set; } = false;

        /// <summary>
        /// Create a dialog that can be used to display information about a procedure that can be canceled.
        /// </summary>
        /// <param name="Title">Title of this window</param>
        /// <param name="Info">The information displayed</param>
        /// <param name="OnCancelButtonClicked">An action that will be called when user presses 'Cancel' button</param>
        public CancelableProcedureDialogue(ustring Title, ustring Info, Action OnCancelButtonClicked)
        {
            int intWidth = 50;
            int intHeight = TextFormatter.MaxLines(Info, intWidth) + 4;
            X = Pos.Center();
            Y = Pos.Center();
            Width = intWidth;
            Height = intHeight;
            base.Title = Title;
            infoLabel.Text = Info;
            cancelButton.Y = intHeight - 3;
            cancelButton.Clicked += OnCancelButtonClicked;
            cancelButton.Clicked += () =>
            {
                IsCanceled = true;
                RequestStop();
            };
            Add(infoLabel, cancelButton);
        }

        readonly Label infoLabel = new()
        {
            LayoutStyle = LayoutStyle.Computed,
            TextAlignment = TextAlignment.Centered,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            AutoSize = false
        };

        readonly Button cancelButton = new()
        {
            Text = "Cancel",
            X= Pos.Center(),
        };
    }
}
