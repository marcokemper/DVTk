// ------------------------------------------------------
// DVTk - The Healthcare Validation Toolkit (www.dvtk.org)
// Copyright � 2009 DVTk
// ------------------------------------------------------
// This file is part of DVTk.
//
// DVTk is free software; you can redistribute it and/or modify it under the terms of the GNU
// Lesser General Public License as published by the Free Software Foundation; either version 3.0
// of the License, or (at your option) any later version. 
// 
// DVTk is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even
// the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser
// General Public License for more details. 
// 
// You should have received a copy of the GNU Lesser General Public License along with this
// library; if not, see <http://www.gnu.org/licenses/>



using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using DvtkHighLevelInterface.Common.Threads;



namespace DvtkHighLevelInterface.Common.UserInterfaces
{
	/// <summary>
	/// This class implements a user control for displaying activity logging from HLI Threads.
	/// This user control is used for the HliForm but can also be used on its own.
	/// </summary>
	public class UserControlActivityLogging : System.Windows.Forms.UserControl, IThreadUserInterface
	{
		//
		// - Generated by Visual Studio -
		//

		private System.Windows.Forms.RichTextBox richTextBoxActivityLogging;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.richTextBoxActivityLogging = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // richTextBoxActivityLogging
            // 
            this.richTextBoxActivityLogging.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxActivityLogging.HideSelection = false;
            this.richTextBoxActivityLogging.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxActivityLogging.Name = "richTextBoxActivityLogging";
            this.richTextBoxActivityLogging.ReadOnly = true;
            this.richTextBoxActivityLogging.Size = new System.Drawing.Size(304, 264);
            this.richTextBoxActivityLogging.TabIndex = 0;
            this.richTextBoxActivityLogging.Text = "";
            // 
            // UserControlActivityLogging
            // 
            this.Controls.Add(this.richTextBoxActivityLogging);
            this.Name = "UserControlActivityLogging";
            this.Size = new System.Drawing.Size(304, 264);
            this.ResumeLayout(false);

		}
		#endregion



		//
		// - Fields -
		//

		/// <summary>
		/// A thread-safe queue, for storing the actions until they are performed when the
		/// timer raises an event.
		/// </summary>
		private Queue actionQueue = Queue.Synchronized(new Queue());

		/// <summary>
		/// Event handler that can handle an error output event from a Thread.
		/// </summary>
		private Thread.TextOutputEventHandler errorOutputEventHandler = null;

		/// <summary>
		/// Event handler that can handle an information output event from a Thread.
		/// </summary>
		private Thread.TextOutputEventHandler informationOutputEventHandler = null;

        /// <summary>
        /// See the method SetNumberOfLines.
        /// </summary>
        private UInt32 keepNumberOfLines = 25000;

		/// <summary>
		/// The last identifier used for logging (is null if no logging has taken place yet).
		/// </summary>
		private String lastIdentifierUsed = null;

        /// <summary>
        /// See the method SetNumberOfLines.
        /// </summary>
        private UInt32 maximumNumberOfLines = 50000;

        /// <summary>
        /// Use this to synchronize the reading and writing of the keepNumberOfLines and maximumNumberOfLines fields.
        /// </summary>
        private System.Threading.ReaderWriterLock numberOfLinesLock = new System.Threading.ReaderWriterLock();

		/// <summary>
		/// Timer that will trigger the actual performing of the actions present in the action queue.
		/// </summary>
		private System.Windows.Forms.Timer timer = null;

		/// <summary>
		/// Event handler that can handle a warning output event from a Thread.
		/// </summary>
		private Thread.TextOutputEventHandler warningOutputEventHandler = null;



		//
		// - Constructors -
		//

		/// <summary>
		/// Default constructor.
        /// <br></br><br></br>
		/// Use this constructor when this control uses its own timer to
		/// perform the actions.
		/// </summary>
		public UserControlActivityLogging()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
           
			errorOutputEventHandler = new Thread.TextOutputEventHandler(this.HandleErrorOutputEvent);
			informationOutputEventHandler = new Thread.TextOutputEventHandler(this.HandleInformationOutputEvent);
			warningOutputEventHandler = new Thread.TextOutputEventHandler(this.HandleWarningOutputEvent);

			this.timer = new System.Windows.Forms.Timer();
			this.timer.Tick += new EventHandler(this.PerformActions);
			Interval = 100;
			this.timer.Start();
		}

		/// <summary>
		/// Constructor.
        /// <br></br><br></br>
		/// Use this constructor when this control uses an external timer to
		/// perform the actions.
		/// </summary>
		/// <param name="timer"></param>
		public UserControlActivityLogging(System.Windows.Forms.Timer timer)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			errorOutputEventHandler = new Thread.TextOutputEventHandler(this.HandleErrorOutputEvent);
			informationOutputEventHandler = new Thread.TextOutputEventHandler(this.HandleInformationOutputEvent);
			warningOutputEventHandler = new Thread.TextOutputEventHandler(this.HandleWarningOutputEvent);

			this.timer = timer;
			this.timer.Tick += new EventHandler(this.PerformActions);
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				this.timer.Stop();

				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}



		//
		// - Properties -
		//

		/// <summary>
		/// The time to wait before performing all the newly added actions in the action queue after
		/// the previously performed actions.
		/// </summary>
		public int Interval
		{
			get
			{
				return(this.timer.Interval);
			}
			set
			{
				this.timer.Interval = value;
			}
		}

        /// <summary>
        /// Returns all the text on rich textbox.
        /// </summary>
        public string LoggingText
        {
            get
            {
                return (this.richTextBoxActivityLogging.Text);
            }            
        }
        

		//
		// - Methods -
		//

		/// <summary>
		/// Add a write action to the action queue.
		/// </summary>
		/// <param name="identifier">The identifier of the Thread.</param>
		/// <param name="text">The text to log.</param>
		/// <param name="color">The color of the text to log.</param>
		public void AddWriteActionToQueue(String identifier, String text, System.Drawing.Color color)
		{
			WriteAction writeAction = new WriteAction(identifier, text, color);
			this.actionQueue.Enqueue(writeAction);
		}

        /// <summary>
        /// Adds a clear action to the action queue.
        /// </summary>
        internal void AddClearActionToQueue()
        {
            ClearAction clearAction = new ClearAction();
            this.actionQueue.Enqueue(clearAction);
        }

		/// <summary>
		/// Attach to a Thread.
        /// <br></br><br></br>
		/// By doing so, this User Control may display information, warning and error output events
		/// from the Thread.
		/// </summary>
		/// <param name="thread">The Thread to attach to.</param>
		public void Attach(Thread thread)
		{
			Attach(thread, true);
		}

		internal virtual void Attach(Thread thread, bool attachUserControlToThread)
		{
			thread.ErrorOutputEvent += this.errorOutputEventHandler;
			thread.InformationOutputEvent += this.informationOutputEventHandler;
			thread.WarningOutputEvent += this.warningOutputEventHandler;

			if (attachUserControlToThread)
			{
				thread.AttachedUserInterfaces.Add(this);
			}
		}

		/// <summary>
		/// Clear the contents of the rich text box.
		/// </summary>
		public void Clear()
		{
			this.richTextBoxActivityLogging.Clear();
		}

        /// <summary>
		/// Handle an error output event from a Thread by adding a WriteAction to the action queue.
		/// </summary>
		/// <param name="thread">The Thread that generated the output event.</param>
		/// <param name="text">The text from the output event.</param>
		private void HandleErrorOutputEvent(Thread thread, String text)
		{
			AddWriteActionToQueue(thread.ThreadOptions.Identifier, text, System.Drawing.Color.Red);
		}

		/// <summary>
		/// Handle an information output event from a Thread by adding a WriteAction to the action queue.
		/// </summary>
		/// <param name="thread">The Thread that generated the output event.</param>
		/// <param name="text">The text from the output event.</param>
		private void HandleInformationOutputEvent(Thread thread, String text)
		{
			AddWriteActionToQueue(thread.ThreadOptions.Identifier, text, System.Drawing.Color.Black);
		}

		/// <summary>
		/// Handle a warning output event from a Thread by adding a WriteAction to the action queue.
		/// </summary>
		/// <param name="thread">The Thread that generated the output event.</param>
		/// <param name="text">The text from the output event.</param>
		private void HandleWarningOutputEvent(Thread thread, String text)
		{
			AddWriteActionToQueue(thread.ThreadOptions.Identifier, text, System.Drawing.Color.Orange);
		}

		/// <summary>
		/// Perform the actions that are stored in the action stack.
        /// <br></br><br></br>
		/// This method handles the timer event.
		/// </summary>
		/// <param name="myObject">The object.</param>
		/// <param name="myEventArgs">The event args.</param>
		private void PerformActions(Object myObject, EventArgs myEventArgs)
		{
            while (this.actionQueue.Count > 0)
            {
                Object action = this.actionQueue.Dequeue();

                if (action is WriteAction)
                {
                    //
                    // Add the text to the user control.
                    //

                    WriteAction writeAction = action as WriteAction;

                    Write(writeAction.identifier, writeAction.text, writeAction.color);


                    //
                    // To limit memory usage, only display a maximum number of lines in this user control.
                    //

                    String [] currentLines = richTextBoxActivityLogging.Lines;

                    this.numberOfLinesLock.AcquireReaderLock(-1);
                    UInt32 keepNumberOfLinesCopy = this.keepNumberOfLines;
                    UInt32 maximumNumberOfLinesCopy = this.maximumNumberOfLines;
                    this.numberOfLinesLock.ReleaseReaderLock();

                    if (currentLines.Length > maximumNumberOfLinesCopy)
                    {
                        //
                        // Determine how many characters needs to be removed.
                        //
                        
                        UInt32 numberOfLinesToRemove = Convert.ToUInt32(currentLines.Length) - keepNumberOfLinesCopy;
                        UInt32 numberOfCharactersToRemove = 0;

                        for (UInt32 counter = 0; counter < numberOfLinesToRemove; counter++)
                        {
                            numberOfCharactersToRemove += Convert.ToUInt32(currentLines[counter].Length) + 1;
                        }


                        //
                        // Remove the characters.
                        //

                        richTextBoxActivityLogging.SelectionStart = 0;
                        richTextBoxActivityLogging.SelectionLength = Convert.ToInt32(numberOfCharactersToRemove);
                        richTextBoxActivityLogging.SelectionColor = Color.Red;
                        richTextBoxActivityLogging.SelectedText = "Part of the logging has been removed.\r\n\r\n";
                        richTextBoxActivityLogging.SelectionColor = Color.Black;

                        richTextBoxActivityLogging.SelectionLength = 0;
                        richTextBoxActivityLogging.SelectionStart = 10000000;
                    }
                }
                else if (action is ClearAction)
                {
                    Clear();
                }
            }
		}

        /// <summary>
        /// Determines how much logging in this instance may be present.
        /// </summary>
        /// <param name="maximumNumberOfLines">
        /// Maximum number of lines displayed in this instance before before the oldest lines will
        /// be removed.</param>
        /// <param name="keepNumberOfLines">
        /// Number of newest lines that will be kept when the oldest lines are removed.
        /// </param>
        public void SetNumberOfLines(UInt32 maximumNumberOfLines, UInt32 keepNumberOfLines)
        {
            this.numberOfLinesLock.AcquireWriterLock(-1);
            this.maximumNumberOfLines = maximumNumberOfLines;
            this.keepNumberOfLines = keepNumberOfLines;
            this.numberOfLinesLock.ReleaseWriterLock();
        }

		/// <summary>
		/// Write the text.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="text">The text.</param>
		/// <param name="color">The color of the text.</param>
		private void Write(String identifier, String text, System.Drawing.Color color)
		{
			// If this identifier has not been used last time, show it.
			if (this.lastIdentifierUsed != identifier)
			{
				this.richTextBoxActivityLogging.SelectionColor = System.Drawing.Color.LightGreen;
				this.richTextBoxActivityLogging.AppendText("-----\r\n");
				this.richTextBoxActivityLogging.SelectionColor = System.Drawing.Color.Blue;
				this.richTextBoxActivityLogging.AppendText("[" + identifier + "]\r\n");
				this.lastIdentifierUsed = identifier;
			}

			this.richTextBoxActivityLogging.SelectionColor = System.Drawing.Color.LightGreen;
			this.richTextBoxActivityLogging.AppendText("-----\r\n");
			this.richTextBoxActivityLogging.SelectionColor = color;
			this.richTextBoxActivityLogging.AppendText(text + "\r\n");

			// Remark: the rich text box will automatically scroll to the end
			// when the HideSelection property is set to false.
		}

		internal class WriteAction
		{
			public System.Drawing.Color color = System.Drawing.Color.Black;

			public String identifier = "";

			public String text = "";

			public WriteAction(String identifier, String text, System.Drawing.Color color)
			{
				this.identifier = identifier;
				this.text = text;
				this.color = color;
			}
		}

        internal class ClearAction
        {

        }
	}
}