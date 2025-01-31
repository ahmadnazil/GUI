#region Copyright
///<remarks>
/// <GRAL Graphical User Interface GUI>
/// Copyright (C) [2019]  [Dietmar Oettl, Markus Kuntner]
/// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by
/// the Free Software Foundation version 3 of the License
/// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
/// You should have received a copy of the GNU General Public License along with this program.  If not, see <https://www.gnu.org/licenses/>.
///</remarks>
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using System.IO;

using Gral;
using GralDomain;
using GralItemData;
using GralStaticFunctions;

namespace GralItemForms
{
    public partial class EditReceptors : Form
    {
        /// <summary>
        /// Collection of all receptor data
        /// </summary>
        public List<ReceptorData> ItemData = new List<ReceptorData>();
        /// <summary>
        /// Number of actual receptor to be displayed
        /// </summary>
        public int ItemDisplayNr;
        private CultureInfo ic = CultureInfo.InvariantCulture;

        // delegate to send a message, that redraw is needed!
        public event ForceDomainRedraw ReceptorRedraw;

        public bool AllowClosing = false;
        //delegates to send a message, that user uses OK or Cancel button
        public event ForceItemOK ItemFormOK;
        public event ForceItemCancel ItemFormCancel;

        public double MinReceptorHeight = 0; // min receptor height from Main - DeltaZ

        private int TextBox_x0 = 0;
        private int TrackBar_x0 = 0;
        private int Numericupdown_x0 = 0;

        public EditReceptors()
        {
            InitializeComponent();

            #if __MonoCS__

                numericUpDown1.TextAlign =  HorizontalAlignment.Left;
                numericUpDown2.TextAlign = HorizontalAlignment.Left;
            #else
            #endif
            MouseMove += new MouseEventHandler(Aktiv);
        }

        private void Aktiv(object sender, MouseEventArgs e)
        {
            Activate();
            RedrawDomain(this, null);
        }

        private void EditReceptors_Load(object sender, EventArgs e)
        {
            textBox2.TextChanged += new System.EventHandler(St_F.CheckInput);
            textBox3.TextChanged += new System.EventHandler(St_F.CheckInput);

            FillValues();
            if (textBox1.Text.Length == 0 && numericUpDown1.Value == 0) // set standard values for the 1st Receptor point
            {
                textBox2.Text = "";
                textBox3.Text = "";
                numericUpDown1.Value = 3;
                numericUpDown2.Value = 0;
            }

            TrackBar_x0 = trackBar1.Left;
            TextBox_x0 = textBox1.Left;
            Numericupdown_x0 = numericUpDown1.Left;

            textBox2.KeyPress += new KeyPressEventHandler(St_F.NumericInput);
            textBox3.KeyPress += new KeyPressEventHandler(St_F.NumericInput);
            numericUpDown1.Minimum = (decimal) (MinReceptorHeight);
        }

        //increase the number of receptors by one
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox2.Text != "")
            {
                SaveArray(false);
                textBox2.Text = "";
                textBox3.Text = "";
                /*
                numericUpDown1.Value = 3; // set standard value for a new Receptor point
                */
                trackBar1.Maximum += 1;
                trackBar1.Value = trackBar1.Maximum;
                ItemDisplayNr = trackBar1.Maximum - 1;
                RedrawDomain(this, null);
            }
        }

        //scroll between the receptors
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            SaveArray(false);
            ItemDisplayNr = trackBar1.Value - 1;
            FillValues();
            RedrawDomain(this, null);
        }

        //save data in array list
        /// <summary>
        /// Saves the recent dialog data in the item object and the item list
        /// </summary>
        public void SaveArray(bool redraw)
        {
            bool valid_coors = true;
            double x = 0;
            double y = 0;
            try
            {
                x = double.Parse(textBox2.Text);
                y = double.Parse(textBox3.Text);
            }
            catch
            {
                valid_coors = false;
            }

            ReceptorData _rdata;
            if (ItemDisplayNr >= ItemData.Count) // new item
            {
                _rdata = new ReceptorData();
            }
            else // change existing item
            {
                _rdata = ItemData[ItemDisplayNr];
            }

            if (textBox1.Text != "" && valid_coors)
            {
                _rdata.Name = St_F.RemoveinvalidChars(textBox1.Text);
                _rdata.Pt = new PointD(x, y);
                _rdata.Height = (float) (numericUpDown1.Value);
                _rdata.DisplayValue = (float) (numericUpDown2.Value);

                if (ItemDisplayNr >= ItemData.Count) // new item
                {
                    ItemData.Add(_rdata);
                }
                else
                {
                    ItemData[ItemDisplayNr] = _rdata;
                }
                if (redraw)
                {
                    RedrawDomain(this, null);
                }
             }
        }

        //fill actual values
        /// <summary>
        /// Fills the dialog with data from the recent item object
        /// </summary>
        public void FillValues()
        {
            ReceptorData _rdata;
            if (ItemDisplayNr < ItemData.Count)
            {
                _rdata = ItemData[ItemDisplayNr];
            }
            else
            {
                if (ItemData.Count > 0)
                {
                    _rdata = new ReceptorData(ItemData[ItemData.Count - 1]);
                }
                else
                {
                    _rdata = new ReceptorData();
                }
            }

            textBox1.Text = _rdata.Name;
            textBox2.Text = _rdata.Pt.X.ToString();
            textBox3.Text = _rdata.Pt.Y.ToString();
            numericUpDown1.Value = St_F.ValueSpan(MinReceptorHeight, 999, _rdata.Height);
            numericUpDown2.Value = St_F.ValueSpan(-1000000, 1000000, _rdata.DisplayValue);
        }

        //remove actual receptor
        private void button2_Click(object sender, EventArgs e)
        {
            RemoveOne(true);
            RedrawDomain(this, null);
        }

        /// <summary>
        /// Remove the recent item object from the item list
        /// </summary>
        public void RemoveOne(bool ask)
        {
            if (ask == true)
            {
                if (St_F.InputBox("Attention", "Do you really want to delete this receptor?", this) == DialogResult.OK)
                {
                    ask = false;
                }
                else
                {
                    ask = true; // Cancel -> do not delete!
                }
            }

            if (ask == false)
            {
                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
                numericUpDown1.Value = numericUpDown1.Minimum;
                numericUpDown2.Value = 0;
                if (ItemDisplayNr >= 0)
                {
                    try
                    {
                        if (trackBar1.Maximum > 1)
                        {
                            trackBar1.Maximum -= 1;
                        }

                        trackBar1.Value = Math.Min(trackBar1.Maximum, trackBar1.Value);
                        ItemData.RemoveAt(ItemDisplayNr);
                        ItemDisplayNr = trackBar1.Value - 1;
                        FillValues();
                    }
                    catch
                    {
                    }
                }
            }
        }

        //remove all receptors
        private void button3_Click(object sender, EventArgs e)
        {
            if (St_F.InputBox("Attention", "Delete all receptors??", this) == DialogResult.OK)
            {
                ItemData.Clear();
                ItemData.TrimExcess(); // Kuntner
                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
                numericUpDown1.Value = numericUpDown1.Minimum;
                numericUpDown2.Value = 0;
                trackBar1.Value = 1;
                trackBar1.Maximum = 1;
                ItemDisplayNr = trackBar1.Value - 1;
                RedrawDomain(this, null);
            }
        }

        private void RedrawDomain(object sender, EventArgs e)
        {
            // send Message to domain Form, that Section-Form is closed
            try
            {
            if (ReceptorRedraw != null)
                {
                    ReceptorRedraw(this, e);
                }
            }
            catch
            {}
        }

        public void ShowForm()
        {
            ItemDisplayNr = trackBar1.Value - 1;
            RedrawDomain(this, null);
        }

        private void EditReceptors_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.ApplicationExitCall || e.CloseReason == CloseReason.WindowsShutDown)
            {
                // allow Closing in this cases
                e.Cancel = false;
            }
            else
            {
                if (((sender as Form).ActiveControl is Button) == false)
                {
                    // cancel if x has been pressed = restore old values!
                    cancelButtonClick(null, null);
                }
                // Hide form and send message to caller when user tries to close this form
                if (!AllowClosing)
                {
                    this.Hide();
                    e.Cancel = true;
                }
            }
        }

        void EditReceptorsFormClosed(object sender, FormClosedEventArgs e)
        {
            MouseMove -= new MouseEventHandler(Aktiv);
            textBox2.TextChanged -= new System.EventHandler(St_F.CheckInput);
            textBox3.TextChanged -= new System.EventHandler(St_F.CheckInput);
            textBox2.KeyPress -= new KeyPressEventHandler(St_F.NumericInput);
            textBox2.KeyPress -= new KeyPressEventHandler(St_F.NumericInput);

            ItemData.Clear();
            ItemData.TrimExcess();
            toolTip1.Dispose();
        }

        private void Paste(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.V))
            {
                string txt = Clipboard.GetText();
                double val = 0;
                try
                {
                    if (double.TryParse(txt, out val))
                    {
                        TextBox tbox = sender as TextBox;
                        tbox.Text = txt;
                        tbox.SelectAll();
                    }
                }
                catch { }
            }
        }

        void Button4Click(object sender, EventArgs e)
        {
            SaveArray(true);
            FillValues();
        }

        void EditReceptorsVisibleChanged(object sender, EventArgs e)
        {
            if (!Visible)
            {
            }
            else // Enable/disable items
            {
                bool enable = !Main.Project_Locked;
                if (enable)
                {
                    labelTitle.Text = "Edit Receptors";
                }
                else
                {
                    labelTitle.Text = "Receptor Settings (Project Locked)";
                }
                foreach (Control c in Controls)
                {
                    if (c != trackBar1 && c != ScrollRight && c != ScrollLeft)
                    {
                        c.Enabled = enable;
                    }
                }
            }
            Exit.Enabled = true;
            panel1.Enabled = true;
        }

        void EditReceptorsResizeEnd(object sender, EventArgs e)
        {
            int dialog_width = ClientSize.Width;
            if (dialog_width > 130)
            {
                dialog_width -= 12;
                trackBar1.Width = ScrollRight.Left - TrackBar_x0;
                textBox1.Width = dialog_width - textBox1.Left;
            }
            panel1.Width = ClientSize.Width;
        }

        public void SetXCoorText(string _s)
        {
            textBox2.Text = _s;
        }
        public void SetYCoorText(string _s)
        {
            textBox3.Text = _s;
        }

        /// <summary>
        /// Set the trackbar to the desired item number
        /// </summary>
        public bool SetTrackBar(int _nr)
        {
            if (_nr >= trackBar1.Minimum && _nr <= trackBar1.Maximum)
            {
                trackBar1.Value = _nr;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Set the trackbar maximum to the maximum count in the item list
        /// </summary>
        public void SetTrackBarMaximum()
        {
            trackBar1.Maximum = Math.Max(ItemData.Count, 1);
        }

        /// <summary>
        /// OK Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            SaveArray(true);
            FillValues();
            // send Message to domain Form, that OK button has been pressed
            try
            {
                if (ItemFormOK != null)
                {
                    ItemFormOK(this, e);
                }
            }
            catch
            { }
        }
        /// <summary>
        /// Cancel button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelButtonClick(object sender, EventArgs e)
        {
            ResetItemData();
            // send Message to domain Form, that Cancel button has been pressed
            try
            {
                if (ItemFormCancel != null)
                {
                    ItemFormCancel(this, e);
                }
            }
            catch
            { }
        }
        /// <summary>
        /// Reset item data when cancelling the dialog
        /// </summary>
        public void ResetItemData()
        {
            ItemData.Clear();
            ItemData.TrimExcess();
            ReceptorDataIO _rd = new ReceptorDataIO();
            _rd.LoadReceptors(ItemData, Path.Combine(Gral.Main.ProjectName, "Computation", "Receptor.dat"));
            _rd = null;
            SetTrackBarMaximum();
            FillValues();
        }

        /// <summary>
        /// Use panel1 to move the form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            const int WM_NCLBUTTONDOWN = 0x00A1;
            const int HTCAPTION = 2;
            panel1.Capture = false;
            labelTitle.Capture = false;
            Message msg = Message.Create(this.Handle, WM_NCLBUTTONDOWN, new IntPtr(HTCAPTION), IntPtr.Zero);
            this.DefWndProc(ref msg);
        }

        private void ScrollRight_Click(object sender, EventArgs e)
        {
            if (trackBar1.Value < trackBar1.Maximum)
            {
                trackBar1.Value++;
                trackBar1_Scroll(null, null);
            }
        }

        private void ScrollLeft_Click(object sender, EventArgs e)
        {
            if (trackBar1.Value > trackBar1.Minimum)
            {
                trackBar1.Value--;
                trackBar1_Scroll(null, null);
            }
        }

    }
}
