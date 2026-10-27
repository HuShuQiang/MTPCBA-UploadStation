namespace 美团PCBA过站上传工具
{
    partial class SetupView
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
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel2 = new TableLayoutPanel();
            Station = new TextBox();
            Field = new TextBox();
            label1 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            Model = new TextBox();
            label5 = new Label();
            Fixid = new TextBox();
            Com = new ComboBox();
            tableLayoutPanel3 = new TableLayoutPanel();
            Save_Btn = new Button();
            Cancel_Btn = new Button();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 0);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel3, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
            tableLayoutPanel1.Size = new Size(800, 450);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(Station, 1, 2);
            tableLayoutPanel2.Controls.Add(Field, 1, 1);
            tableLayoutPanel2.Controls.Add(label1, 0, 0);
            tableLayoutPanel2.Controls.Add(label4, 0, 3);
            tableLayoutPanel2.Controls.Add(label3, 0, 2);
            tableLayoutPanel2.Controls.Add(label2, 0, 1);
            tableLayoutPanel2.Controls.Add(Model, 1, 0);
            tableLayoutPanel2.Controls.Add(label5, 0, 4);
            tableLayoutPanel2.Controls.Add(Fixid, 1, 3);
            tableLayoutPanel2.Controls.Add(Com, 1, 4);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(3, 3);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 5;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.Size = new Size(794, 344);
            tableLayoutPanel2.TabIndex = 0;
            // 
            // Station
            // 
            Station.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            Station.Location = new Point(203, 144);
            Station.Name = "Station";
            Station.Size = new Size(588, 51);
            Station.TabIndex = 7;
            Station.Text = "ITP_test";
            // 
            // Field
            // 
            Field.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            Field.Location = new Point(203, 76);
            Field.Name = "Field";
            Field.Size = new Size(588, 51);
            Field.TabIndex = 6;
            Field.Text = "main_board";
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Location = new Point(101, 11);
            label1.Name = "label1";
            label1.Size = new Size(96, 45);
            label1.TabIndex = 0;
            label1.Text = "机型:";
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Right;
            label4.AutoSize = true;
            label4.Location = new Point(101, 215);
            label4.Name = "label4";
            label4.Size = new Size(96, 45);
            label4.TabIndex = 3;
            label4.Text = "工位:";
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Right;
            label3.AutoSize = true;
            label3.Location = new Point(101, 147);
            label3.Name = "label3";
            label3.Size = new Size(96, 45);
            label3.TabIndex = 2;
            label3.Text = "工站:";
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Location = new Point(101, 79);
            label2.Name = "label2";
            label2.Size = new Size(96, 45);
            label2.TabIndex = 1;
            label2.Text = "字段:";
            // 
            // Model
            // 
            Model.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            Model.Location = new Point(203, 8);
            Model.Name = "Model";
            Model.Size = new Size(588, 51);
            Model.TabIndex = 5;
            Model.Text = "T8FJ";
            // 
            // label5
            // 
            label5.Anchor = AnchorStyles.Right;
            label5.AutoSize = true;
            label5.Location = new Point(101, 285);
            label5.Name = "label5";
            label5.Size = new Size(96, 45);
            label5.TabIndex = 4;
            label5.Text = "串口:";
            // 
            // Fixid
            // 
            Fixid.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            Fixid.Location = new Point(203, 212);
            Fixid.Name = "Fixid";
            Fixid.Size = new Size(588, 51);
            Fixid.TabIndex = 8;
            Fixid.Text = "8FZDX0001";
            // 
            // Com
            // 
            Com.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            Com.DropDownStyle = ComboBoxStyle.DropDownList;
            Com.FormattingEnabled = true;
            Com.Location = new Point(203, 295);
            Com.Name = "Com";
            Com.Size = new Size(588, 51);
            Com.TabIndex = 9;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 2;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.Controls.Add(Save_Btn, 0, 0);
            tableLayoutPanel3.Controls.Add(Cancel_Btn, 1, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(3, 353);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 1;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.Size = new Size(794, 94);
            tableLayoutPanel3.TabIndex = 1;
            // 
            // Save_Btn
            // 
            Save_Btn.BackColor = Color.GreenYellow;
            Save_Btn.Dock = DockStyle.Fill;
            Save_Btn.Font = new Font("微软雅黑", 25F);
            Save_Btn.Location = new Point(3, 3);
            Save_Btn.Name = "Save_Btn";
            Save_Btn.Size = new Size(391, 88);
            Save_Btn.TabIndex = 0;
            Save_Btn.Text = "保存";
            Save_Btn.UseVisualStyleBackColor = false;
            // 
            // Cancel_Btn
            // 
            Cancel_Btn.BackColor = Color.OrangeRed;
            Cancel_Btn.Dock = DockStyle.Fill;
            Cancel_Btn.Font = new Font("微软雅黑", 25F);
            Cancel_Btn.Location = new Point(400, 3);
            Cancel_Btn.Name = "Cancel_Btn";
            Cancel_Btn.Size = new Size(391, 88);
            Cancel_Btn.TabIndex = 1;
            Cancel_Btn.Text = "取消";
            Cancel_Btn.UseVisualStyleBackColor = false;
            // 
            // SetupView
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(800, 450);
            Controls.Add(tableLayoutPanel1);
            Font = new Font("微软雅黑", 25F);
            MaximizeBox = false;
            Name = "SetupView";
            StartPosition = FormStartPosition.CenterParent;
            Text = "SetupView";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            tableLayoutPanel3.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private TableLayoutPanel tableLayoutPanel3;
        private Button Save_Btn;
        private Button Cancel_Btn;
        private Label label1;
        private Label label5;
        private Label label4;
        private Label label3;
        private Label label2;
        private TextBox Model;
        private TextBox Fixid;
        private TextBox Station;
        private TextBox Field;
        private ComboBox Com;
    }
}