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
            this.textBox1.Text = "C:\\temp\\XMLandObjects.DMP";
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
            this.textBox3.Location = new System.Drawing.Point(2, 194);
            this.textBox3.Multiline = true;
            this.textBox3.Name = "textBox3";
            this.textBox3.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox3.Size = new System.Drawing.Size(1911, 570);
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
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1914, 834);
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
    }
}

