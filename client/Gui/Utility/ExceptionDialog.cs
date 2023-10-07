/*  
 *  Namespace   :   client.Gui
 *  Filename    :   ExceptionDialog.cs
 *  Class       :   ExceptionDialog
 *  
 *  Creator     :   Nictheboy
 *  Create at   :   2023/08/22
 *  Last Modify :   2023/10/07
 *  
 */

using NStack;
using Terminal.Gui;

namespace client.Gui
{
    /// <summary>
    /// A dialog that shows an exception.
    /// </summary>
    internal static class ExceptionDialog
    {
        /// <summary>
        /// Shows an exception with GUI.
        /// </summary>
        /// <param name="e">The exception to show</param>
        public static void Show(Exception e)
        {
            Application.MainLoop.Invoke(() =>
            {
                /*
                int result = MessageBox.ErrorQuery("Exception", e.Message, new ustring[] { "Stack trace", "Abort" });
                if (result == 0)
                {
                    MessageBox.ErrorQuery("Stack trace", e.StackTrace, "OK");
                }
                */
                MessageBox.ErrorQuery("Exception", e.ToString(), new ustring[] { "Abort" });
            });
        }
    }
}
