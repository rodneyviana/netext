namespace ProofOfConcept
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.button4 = new System.Windows.Forms.Button();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.button5 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.button7 = new System.Windows.Forms.Button();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.button8 = new System.Windows.Forms.Button();
            this.button9 = new System.Windows.Forms.Button();
            this.button10 = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox7 = new System.Windows.Forms.TextBox();
            this.textBox8 = new System.Windows.Forms.TextBox();
            this.button11 = new System.Windows.Forms.Button();
            this.button12 = new System.Windows.Forms.Button();
            this.button13 = new System.Windows.Forms.Button();
            this.button14 = new System.Windows.Forms.Button();
            this.textBox9 = new System.Windows.Forms.TextBox();
            this.button17 = new System.Windows.Forms.Button();
            this.button15 = new System.Windows.Forms.Button();
            this.checkGrid = new System.Windows.Forms.CheckBox();
            this.checkNoMS = new System.Windows.Forms.CheckBox();
            this.textBox10 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.checkOrder = new System.Windows.Forms.CheckBox();
            this.checkPath = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox11 = new System.Windows.Forms.TextBox();
            this.checkManaged = new System.Windows.Forms.CheckBox();
            this.checkDebug = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox12 = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox13 = new System.Windows.Forms.TextBox();
            this.checkByNane = new System.Windows.Forms.CheckBox();
            this.button16 = new System.Windows.Forms.Button();
            this.button18 = new System.Windows.Forms.Button();
            this.textAssemby = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Dump file:";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(96, 29);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(542, 22);
            this.textBox1.TabIndex = 1;
            this.textBox1.Text = "C:\\temp\\OWSTIMER.dmp";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(676, 16);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(63, 35);
            this.button1.TabIndex = 2;
            this.button1.Text = "...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.DefaultExt = "dmp";
            this.openFileDialog1.Filter = "Dump File|*.dmp|All files|*.*";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(19, 82);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(93, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "Filter by type:";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(136, 78);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(432, 22);
            this.textBox2.TabIndex = 4;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(22, 131);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(167, 39);
            this.button2.TabIndex = 5;
            this.button2.Text = "Get Next";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(195, 131);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(167, 39);
            this.button3.TabIndex = 6;
            this.button3.Text = "Dump Last";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // textBox3
            // 
            this.textBox3.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox3.Location = new System.Drawing.Point(-7, 283);
            this.textBox3.Multiline = true;
            this.textBox3.Name = "textBox3";
            this.textBox3.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox3.Size = new System.Drawing.Size(1911, 519);
            this.textBox3.TabIndex = 7;
            this.textBox3.WordWrap = false;
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(368, 131);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(167, 39);
            this.button4.TabIndex = 8;
            this.button4.Text = "Dump This";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // textBox4
            // 
            this.textBox4.Location = new System.Drawing.Point(541, 148);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(201, 22);
            this.textBox4.TabIndex = 9;
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(591, 77);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(105, 25);
            this.button5.TabIndex = 10;
            this.button5.Text = "Index";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(882, 74);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(135, 25);
            this.button6.TabIndex = 11;
            this.button6.Text = "Get From MT";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // textBox5
            // 
            this.textBox5.Location = new System.Drawing.Point(1035, 75);
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new System.Drawing.Size(229, 22);
            this.textBox5.TabIndex = 12;
            // 
            // button7
            // 
            this.button7.Location = new System.Drawing.Point(886, 118);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(130, 26);
            this.button7.TabIndex = 13;
            this.button7.Text = "Get From EE";
            this.button7.UseVisualStyleBackColor = true;
            this.button7.Click += new System.EventHandler(this.button7_Click);
            // 
            // textBox6
            // 
            this.textBox6.Location = new System.Drawing.Point(1035, 118);
            this.textBox6.Name = "textBox6";
            this.textBox6.Size = new System.Drawing.Size(229, 22);
            this.textBox6.TabIndex = 14;
            // 
            // button8
            // 
            this.button8.Location = new System.Drawing.Point(773, 15);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(169, 32);
            this.button8.TabIndex = 15;
            this.button8.Text = "Heap Enum";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.button8_Click);
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(961, 16);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(152, 31);
            this.button9.TabIndex = 16;
            this.button9.Text = "Check for update";
            this.button9.UseVisualStyleBackColor = true;
            this.button9.Click += new System.EventHandler(this.button9_Click);
            // 
            // button10
            // 
            this.button10.Location = new System.Drawing.Point(769, 149);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(117, 33);
            this.button10.TabIndex = 17;
            this.button10.Text = "Dump Handles";
            this.button10.UseVisualStyleBackColor = true;
            this.button10.Click += new System.EventHandler(this.button10_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(893, 148);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(70, 21);
            this.checkBox1.TabIndex = 18;
            this.checkBox1.Text = "Group";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(969, 157);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(51, 17);
            this.label3.TabIndex = 19;
            this.label3.Text = "handle";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(1140, 159);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(40, 17);
            this.label4.TabIndex = 20;
            this.label4.Text = "Type";
            // 
            // textBox7
            // 
            this.textBox7.Location = new System.Drawing.Point(1026, 156);
            this.textBox7.Name = "textBox7";
            this.textBox7.Size = new System.Drawing.Size(108, 22);
            this.textBox7.TabIndex = 21;
            // 
            // textBox8
            // 
            this.textBox8.Location = new System.Drawing.Point(1186, 157);
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new System.Drawing.Size(195, 22);
            this.textBox8.TabIndex = 22;
            // 
            // button11
            // 
            this.button11.Location = new System.Drawing.Point(591, 108);
            this.button11.Name = "button11";
            this.button11.Size = new System.Drawing.Size(114, 22);
            this.button11.TabIndex = 23;
            this.button11.Text = "Threads";
            this.button11.UseVisualStyleBackColor = true;
            this.button11.Click += new System.EventHandler(this.button11_Click);
            // 
            // button12
            // 
            this.button12.Location = new System.Drawing.Point(734, 74);
            this.button12.Name = "button12";
            this.button12.Size = new System.Drawing.Size(111, 23);
            this.button12.TabIndex = 24;
            this.button12.Text = "All Exceptions";
            this.button12.UseVisualStyleBackColor = true;
            this.button12.Click += new System.EventHandler(this.button12_Click);
            // 
            // button13
            // 
            this.button13.Location = new System.Drawing.Point(734, 108);
            this.button13.Name = "button13";
            this.button13.Size = new System.Drawing.Size(111, 32);
            this.button13.TabIndex = 25;
            this.button13.Text = "Class";
            this.button13.UseVisualStyleBackColor = true;
            this.button13.Click += new System.EventHandler(this.button13_Click);
            // 
            // button14
            // 
            this.button14.Location = new System.Drawing.Point(1132, 18);
            this.button14.Name = "button14";
            this.button14.Size = new System.Drawing.Size(165, 27);
            this.button14.TabIndex = 26;
            this.button14.Text = "String Stat";
            this.button14.UseVisualStyleBackColor = true;
            this.button14.Click += new System.EventHandler(this.button14_Click);
            // 
            // textBox9
            // 
            this.textBox9.Location = new System.Drawing.Point(1438, 78);
            this.textBox9.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBox9.Name = "textBox9";
            this.textBox9.Size = new System.Drawing.Size(180, 22);
            this.textBox9.TabIndex = 27;
            // 
            // button17
            // 
            this.button17.Location = new System.Drawing.Point(1276, 75);
            this.button17.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button17.Name = "button17";
            this.button17.Size = new System.Drawing.Size(150, 33);
            this.button17.TabIndex = 30;
            this.button17.Text = "Xml Doc";
            this.button17.UseVisualStyleBackColor = true;
            this.button17.Click += new System.EventHandler(this.button17_Click);
            // 
            // button15
            // 
            this.button15.Location = new System.Drawing.Point(22, 185);
            this.button15.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button15.Name = "button15";
            this.button15.Size = new System.Drawing.Size(167, 34);
            this.button15.TabIndex = 31;
            this.button15.Text = "Dump Modules";
            this.button15.UseVisualStyleBackColor = true;
            this.button15.Click += new System.EventHandler(this.button15_Click);
            // 
            // checkGrid
            // 
            this.checkGrid.AutoSize = true;
            this.checkGrid.Location = new System.Drawing.Point(209, 176);
            this.checkGrid.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkGrid.Name = "checkGrid";
            this.checkGrid.Size = new System.Drawing.Size(57, 21);
            this.checkGrid.TabIndex = 32;
            this.checkGrid.Text = "Grid";
            this.checkGrid.UseVisualStyleBackColor = true;
            // 
            // checkNoMS
            // 
            this.checkNoMS.AutoSize = true;
            this.checkNoMS.Location = new System.Drawing.Point(209, 207);
            this.checkNoMS.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkNoMS.Name = "checkNoMS";
            this.checkNoMS.Size = new System.Drawing.Size(140, 21);
            this.checkNoMS.TabIndex = 33;
            this.checkNoMS.Text = "Exclude Microsoft";
            this.checkNoMS.UseVisualStyleBackColor = true;
            // 
            // textBox10
            // 
            this.textBox10.Location = new System.Drawing.Point(448, 202);
            this.textBox10.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBox10.Name = "textBox10";
            this.textBox10.Size = new System.Drawing.Size(120, 22);
            this.textBox10.TabIndex = 34;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(374, 207);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(54, 17);
            this.label5.TabIndex = 35;
            this.label5.Text = "Pattern";
            // 
            // checkOrder
            // 
            this.checkOrder.AutoSize = true;
            this.checkOrder.Location = new System.Drawing.Point(378, 178);
            this.checkOrder.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkOrder.Name = "checkOrder";
            this.checkOrder.Size = new System.Drawing.Size(67, 21);
            this.checkOrder.TabIndex = 36;
            this.checkOrder.Text = "Order";
            this.checkOrder.UseVisualStyleBackColor = true;
            // 
            // checkPath
            // 
            this.checkPath.AutoSize = true;
            this.checkPath.Location = new System.Drawing.Point(586, 178);
            this.checkPath.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkPath.Name = "checkPath";
            this.checkPath.Size = new System.Drawing.Size(108, 21);
            this.checkPath.TabIndex = 37;
            this.checkPath.Text = "Include Path";
            this.checkPath.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(582, 205);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(67, 17);
            this.label6.TabIndex = 39;
            this.label6.Text = "Company";
            // 
            // textBox11
            // 
            this.textBox11.Location = new System.Drawing.Point(655, 200);
            this.textBox11.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBox11.Name = "textBox11";
            this.textBox11.Size = new System.Drawing.Size(120, 22);
            this.textBox11.TabIndex = 38;
            // 
            // checkManaged
            // 
            this.checkManaged.AutoSize = true;
            this.checkManaged.Location = new System.Drawing.Point(791, 200);
            this.checkManaged.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkManaged.Name = "checkManaged";
            this.checkManaged.Size = new System.Drawing.Size(122, 21);
            this.checkManaged.TabIndex = 40;
            this.checkManaged.Text = "Managed Only";
            this.checkManaged.UseVisualStyleBackColor = true;
            // 
            // checkDebug
            // 
            this.checkDebug.AutoSize = true;
            this.checkDebug.Location = new System.Drawing.Point(933, 200);
            this.checkDebug.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkDebug.Name = "checkDebug";
            this.checkDebug.Size = new System.Drawing.Size(111, 21);
            this.checkDebug.TabIndex = 41;
            this.checkDebug.Text = "Debug Mode";
            this.checkDebug.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(1105, 200);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(98, 17);
            this.label7.TabIndex = 42;
            this.label7.Text = "Save Module: ";
            // 
            // textBox12
            // 
            this.textBox12.Location = new System.Drawing.Point(1218, 198);
            this.textBox12.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBox12.Name = "textBox12";
            this.textBox12.Size = new System.Drawing.Size(171, 22);
            this.textBox12.TabIndex = 43;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(1416, 202);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(37, 17);
            this.label8.TabIndex = 44;
            this.label8.Text = "Path";
            // 
            // textBox13
            // 
            this.textBox13.Location = new System.Drawing.Point(1460, 198);
            this.textBox13.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBox13.Name = "textBox13";
            this.textBox13.Size = new System.Drawing.Size(172, 22);
            this.textBox13.TabIndex = 45;
            this.textBox13.Text = "c:\\temp\\libs";
            // 
            // checkByNane
            // 
            this.checkByNane.AutoSize = true;
            this.checkByNane.Location = new System.Drawing.Point(1657, 198);
            this.checkByNane.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkByNane.Name = "checkByNane";
            this.checkByNane.Size = new System.Drawing.Size(83, 21);
            this.checkByNane.TabIndex = 46;
            this.checkByNane.Text = "ByName";
            this.checkByNane.UseVisualStyleBackColor = true;
            // 
            // button16
            // 
            this.button16.Location = new System.Drawing.Point(1747, 191);
            this.button16.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button16.Name = "button16";
            this.button16.Size = new System.Drawing.Size(103, 31);
            this.button16.TabIndex = 47;
            this.button16.Text = "Save Module";
            this.button16.UseVisualStyleBackColor = true;
            this.button16.Click += new System.EventHandler(this.button16_Click);
            // 
            // button18
            // 
            this.button18.Location = new System.Drawing.Point(22, 235);
            this.button18.Name = "button18";
            this.button18.Size = new System.Drawing.Size(167, 28);
            this.button18.TabIndex = 48;
            this.button18.Text = "Show Reflection";
            this.button18.UseVisualStyleBackColor = true;
            this.button18.Click += new System.EventHandler(this.button18_Click);
            // 
            // textAssemby
            // 
            this.textAssemby.Location = new System.Drawing.Point(207, 235);
            this.textAssemby.Name = "textAssemby";
            this.textAssemby.Size = new System.Drawing.Size(237, 22);
            this.textAssemby.TabIndex = 49;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1914, 834);
            this.Controls.Add(this.textAssemby);
            this.Controls.Add(this.button18);
            this.Controls.Add(this.button16);
            this.Controls.Add(this.checkByNane);
            this.Controls.Add(this.textBox13);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.textBox12);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.checkDebug);
            this.Controls.Add(this.checkManaged);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox11);
            this.Controls.Add(this.checkPath);
            this.Controls.Add(this.checkOrder);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBox10);
            this.Controls.Add(this.checkNoMS);
            this.Controls.Add(this.checkGrid);
            this.Controls.Add(this.button15);
            this.Controls.Add(this.button17);
            this.Controls.Add(this.textBox9);
            this.Controls.Add(this.button14);
            this.Controls.Add(this.button13);
            this.Controls.Add(this.button12);
            this.Controls.Add(this.button11);
            this.Controls.Add(this.textBox8);
            this.Controls.Add(this.textBox7);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.button10);
            this.Controls.Add(this.button9);
            this.Controls.Add(this.button8);
            this.Controls.Add(this.textBox6);
            this.Controls.Add(this.button7);
            this.Controls.Add(this.textBox5);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.textBox4);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.TextBox textBox6;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Button button10;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox7;
        private System.Windows.Forms.TextBox textBox8;
        private System.Windows.Forms.Button button11;
        private System.Windows.Forms.Button button12;
        private System.Windows.Forms.Button button13;
        private System.Windows.Forms.Button button14;
        private System.Windows.Forms.TextBox textBox9;
        private System.Windows.Forms.Button button17;
        private System.Windows.Forms.Button button15;
        private System.Windows.Forms.CheckBox checkGrid;
        private System.Windows.Forms.CheckBox checkNoMS;
        private System.Windows.Forms.TextBox textBox10;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox checkOrder;
        private System.Windows.Forms.CheckBox checkPath;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox11;
        private System.Windows.Forms.CheckBox checkManaged;
        private System.Windows.Forms.CheckBox checkDebug;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox12;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox13;
        private System.Windows.Forms.CheckBox checkByNane;
        private System.Windows.Forms.Button button16;
        private System.Windows.Forms.Button button18;
        private System.Windows.Forms.TextBox textAssemby;
    }
}

