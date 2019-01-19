namespace SDRSharp.SatnogsTracker
{
    partial class Controlpanel
    {
        /// <summary>
        /// Satnogs Tracker Control Panel
        /// </summary>
        private System.ComponentModel.IContainer components = null;

       /// <summary>
       /// Dispose plugin
       /// </summary>
       /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Control Panel Forms

        /// <summary>
        /// Control Panel Forms
        /// </summary>
        private void InitializeComponent()
        {
            this.checkBoxEnable = new System.Windows.Forms.CheckBox();
            this.labelVersion = new System.Windows.Forms.LinkLabel();
            this.labelDescSatPC32 = new System.Windows.Forms.Label();
            this.labelDescName = new System.Windows.Forms.Label();
            this.labelDescDownlink = new System.Windows.Forms.Label();
            this.labelDescAzimuth = new System.Windows.Forms.Label();
            this.labelDescElevation = new System.Windows.Forms.Label();
            this.labelDescModulation = new System.Windows.Forms.Label();
            this.labelDescBandwidth = new System.Windows.Forms.Label();
            this.checkBoxRecordBase = new System.Windows.Forms.CheckBox();
            this.labelName = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.labelDownlink = new System.Windows.Forms.Label();
            this.labelAzimuth = new System.Windows.Forms.Label();
            this.labelElevation = new System.Windows.Forms.Label();
            this.labelModulation = new System.Windows.Forms.Label();
            this.labelBandwidth = new System.Windows.Forms.Label();
            this.labelSatPC32Status = new System.Windows.Forms.Label();
            this.labelDescSatnogsID = new System.Windows.Forms.Label();
            this.labelSatNogsID = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBoxRecordAF = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // checkBoxEnable
            // 
            this.checkBoxEnable.AutoSize = true;
            this.checkBoxEnable.Location = new System.Drawing.Point(6, 6);
            this.checkBoxEnable.Margin = new System.Windows.Forms.Padding(6);
            this.checkBoxEnable.Name = "checkBoxEnable";
            this.checkBoxEnable.Size = new System.Drawing.Size(109, 29);
            this.checkBoxEnable.TabIndex = 1;
            this.checkBoxEnable.Text = "enable";
            this.checkBoxEnable.UseVisualStyleBackColor = true;
            // 
            // labelVersion
            // 
            this.labelVersion.AutoSize = true;
            this.labelVersion.Location = new System.Drawing.Point(166, 6);
            this.labelVersion.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(53, 25);
            this.labelVersion.TabIndex = 8;
            this.labelVersion.TabStop = true;
            this.labelVersion.Text = "v0.0";
            // 
            // labelDescSatPC32
            // 
            this.labelDescSatPC32.AutoSize = true;
            this.labelDescSatPC32.Location = new System.Drawing.Point(1, 74);
            this.labelDescSatPC32.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelDescSatPC32.Name = "labelDescSatPC32";
            this.labelDescSatPC32.Size = new System.Drawing.Size(103, 25);
            this.labelDescSatPC32.TabIndex = 9;
            this.labelDescSatPC32.Text = "SatPC32:";
            // 
            // labelDescName
            // 
            this.labelDescName.AutoSize = true;
            this.labelDescName.Location = new System.Drawing.Point(45, 118);
            this.labelDescName.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelDescName.Name = "labelDescName";
            this.labelDescName.Size = new System.Drawing.Size(80, 25);
            this.labelDescName.TabIndex = 10;
            this.labelDescName.Text = "Name :";
            // 
            // labelDescDownlink
            // 
            this.labelDescDownlink.AutoSize = true;
            this.labelDescDownlink.Location = new System.Drawing.Point(20, 155);
            this.labelDescDownlink.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelDescDownlink.Name = "labelDescDownlink";
            this.labelDescDownlink.Size = new System.Drawing.Size(105, 25);
            this.labelDescDownlink.TabIndex = 11;
            this.labelDescDownlink.Text = "Downlink:";
            // 
            // labelDescAzimuth
            // 
            this.labelDescAzimuth.AutoSize = true;
            this.labelDescAzimuth.Location = new System.Drawing.Point(30, 191);
            this.labelDescAzimuth.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelDescAzimuth.Name = "labelDescAzimuth";
            this.labelDescAzimuth.Size = new System.Drawing.Size(95, 25);
            this.labelDescAzimuth.TabIndex = 12;
            this.labelDescAzimuth.Text = "Azimuth:";
            // 
            // labelDescElevation
            // 
            this.labelDescElevation.AutoSize = true;
            this.labelDescElevation.Location = new System.Drawing.Point(18, 226);
            this.labelDescElevation.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelDescElevation.Name = "labelDescElevation";
            this.labelDescElevation.Size = new System.Drawing.Size(107, 25);
            this.labelDescElevation.TabIndex = 13;
            this.labelDescElevation.Text = "Elevation:";
            // 
            // labelDescModulation
            // 
            this.labelDescModulation.AutoSize = true;
            this.labelDescModulation.Location = new System.Drawing.Point(1, 263);
            this.labelDescModulation.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelDescModulation.Name = "labelDescModulation";
            this.labelDescModulation.Size = new System.Drawing.Size(124, 25);
            this.labelDescModulation.TabIndex = 14;
            this.labelDescModulation.Text = "Modulation:";
            // 
            // labelDescBandwidth
            // 
            this.labelDescBandwidth.AutoSize = true;
            this.labelDescBandwidth.Location = new System.Drawing.Point(7, 300);
            this.labelDescBandwidth.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelDescBandwidth.Name = "labelDescBandwidth";
            this.labelDescBandwidth.Size = new System.Drawing.Size(118, 25);
            this.labelDescBandwidth.TabIndex = 15;
            this.labelDescBandwidth.Text = "Bandwidth:";
            // 
            // checkBoxRecordBase
            // 
            this.checkBoxRecordBase.AutoSize = true;
            this.checkBoxRecordBase.Location = new System.Drawing.Point(29, 380);
            this.checkBoxRecordBase.Margin = new System.Windows.Forms.Padding(6);
            this.checkBoxRecordBase.Name = "checkBoxRecordBase";
            this.checkBoxRecordBase.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.checkBoxRecordBase.Size = new System.Drawing.Size(168, 29);
            this.checkBoxRecordBase.TabIndex = 17;
            this.checkBoxRecordBase.Text = "Record Base";
            this.checkBoxRecordBase.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.checkBoxRecordBase.UseVisualStyleBackColor = true;
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.Location = new System.Drawing.Point(166, 118);
            this.labelName.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(106, 25);
            this.labelName.TabIndex = 18;
            this.labelName.Text = "Sat Name";
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(0, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(100, 23);
            this.label8.TabIndex = 0;
            // 
            // labelDownlink
            // 
            this.labelDownlink.AutoSize = true;
            this.labelDownlink.Location = new System.Drawing.Point(166, 155);
            this.labelDownlink.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelDownlink.Name = "labelDownlink";
            this.labelDownlink.Size = new System.Drawing.Size(143, 25);
            this.labelDownlink.TabIndex = 19;
            this.labelDownlink.Text = "DownlinkFreq";
            // 
            // labelAzimuth
            // 
            this.labelAzimuth.AutoSize = true;
            this.labelAzimuth.Location = new System.Drawing.Point(166, 191);
            this.labelAzimuth.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelAzimuth.Name = "labelAzimuth";
            this.labelAzimuth.Size = new System.Drawing.Size(37, 25);
            this.labelAzimuth.TabIndex = 20;
            this.labelAzimuth.Text = "Az";
            // 
            // labelElevation
            // 
            this.labelElevation.AutoSize = true;
            this.labelElevation.Location = new System.Drawing.Point(166, 226);
            this.labelElevation.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelElevation.Name = "labelElevation";
            this.labelElevation.Size = new System.Drawing.Size(31, 25);
            this.labelElevation.TabIndex = 21;
            this.labelElevation.Text = "El";
            // 
            // labelModulation
            // 
            this.labelModulation.AutoSize = true;
            this.labelModulation.Location = new System.Drawing.Point(166, 263);
            this.labelModulation.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelModulation.Name = "labelModulation";
            this.labelModulation.Size = new System.Drawing.Size(118, 25);
            this.labelModulation.TabIndex = 22;
            this.labelModulation.Text = "Modulation";
            // 
            // labelBandwidth
            // 
            this.labelBandwidth.AutoSize = true;
            this.labelBandwidth.Location = new System.Drawing.Point(166, 300);
            this.labelBandwidth.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelBandwidth.Name = "labelBandwidth";
            this.labelBandwidth.Size = new System.Drawing.Size(112, 25);
            this.labelBandwidth.TabIndex = 23;
            this.labelBandwidth.Text = "Bandwidth";
            // 
            // labelSatPC32Status
            // 
            this.labelSatPC32Status.AutoSize = true;
            this.labelSatPC32Status.Location = new System.Drawing.Point(166, 74);
            this.labelSatPC32Status.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelSatPC32Status.Name = "labelSatPC32Status";
            this.labelSatPC32Status.Size = new System.Drawing.Size(158, 25);
            this.labelSatPC32Status.TabIndex = 24;
            this.labelSatPC32Status.Text = "SatPC32Status";
            // 
            // labelDescSatnogsID
            // 
            this.labelDescSatnogsID.AutoSize = true;
            this.labelDescSatnogsID.Location = new System.Drawing.Point(7, 342);
            this.labelDescSatnogsID.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelDescSatnogsID.Name = "labelDescSatnogsID";
            this.labelDescSatnogsID.Size = new System.Drawing.Size(117, 25);
            this.labelDescSatnogsID.TabIndex = 25;
            this.labelDescSatnogsID.Text = "SatnogsID:";
            // 
            // labelSatNogsID
            // 
            this.labelSatNogsID.AutoSize = true;
            this.labelSatNogsID.Location = new System.Drawing.Point(166, 342);
            this.labelSatNogsID.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.labelSatNogsID.Name = "labelSatNogsID";
            this.labelSatNogsID.Size = new System.Drawing.Size(32, 25);
            this.labelSatNogsID.TabIndex = 26;
            this.labelSatNogsID.Text = "ID";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBoxRecordAF);
            this.groupBox1.Location = new System.Drawing.Point(0, 44);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(415, 473);
            this.groupBox1.TabIndex = 27;
            this.groupBox1.TabStop = false;
            // 
            // checkBoxRecordAF
            // 
            this.checkBoxRecordAF.AutoSize = true;
            this.checkBoxRecordAF.Location = new System.Drawing.Point(50, 377);
            this.checkBoxRecordAF.Margin = new System.Windows.Forms.Padding(6);
            this.checkBoxRecordAF.Name = "checkBoxRecordAF";
            this.checkBoxRecordAF.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.checkBoxRecordAF.Size = new System.Drawing.Size(146, 29);
            this.checkBoxRecordAF.TabIndex = 28;
            this.checkBoxRecordAF.Text = "Record AF";
            this.checkBoxRecordAF.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.checkBoxRecordAF.UseVisualStyleBackColor = true;
            // 
            // Controlpanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.labelSatNogsID);
            this.Controls.Add(this.labelDescSatnogsID);
            this.Controls.Add(this.labelSatPC32Status);
            this.Controls.Add(this.labelBandwidth);
            this.Controls.Add(this.labelModulation);
            this.Controls.Add(this.labelElevation);
            this.Controls.Add(this.labelAzimuth);
            this.Controls.Add(this.labelDownlink);
            this.Controls.Add(this.labelName);
            this.Controls.Add(this.checkBoxRecordBase);
            this.Controls.Add(this.labelDescBandwidth);
            this.Controls.Add(this.labelDescModulation);
            this.Controls.Add(this.labelDescElevation);
            this.Controls.Add(this.labelDescAzimuth);
            this.Controls.Add(this.labelDescDownlink);
            this.Controls.Add(this.labelDescName);
            this.Controls.Add(this.labelDescSatPC32);
            this.Controls.Add(this.labelVersion);
            this.Controls.Add(this.checkBoxEnable);
            this.Controls.Add(this.groupBox1);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.MinimumSize = new System.Drawing.Size(370, 106);
            this.Name = "Controlpanel";
            this.Size = new System.Drawing.Size(432, 531);
            this.Load += new System.EventHandler(this.Controlpanel_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        public System.Windows.Forms.CheckBox checkBoxEnable;
        private System.Windows.Forms.LinkLabel labelVersion;
        private System.Windows.Forms.Label labelDescSatPC32;
        private System.Windows.Forms.Label labelDescName;
        private System.Windows.Forms.Label labelDescDownlink;
        private System.Windows.Forms.Label labelDescAzimuth;
        private System.Windows.Forms.Label labelDescElevation;
        private System.Windows.Forms.Label labelDescModulation;
        private System.Windows.Forms.Label labelDescBandwidth;
        public System.Windows.Forms.CheckBox checkBoxRecordBase;
        public System.Windows.Forms.Label labelName;
        public System.Windows.Forms.Label label8;
        public System.Windows.Forms.Label labelDownlink;
        public System.Windows.Forms.Label labelAzimuth;
        public System.Windows.Forms.Label labelElevation;
        public System.Windows.Forms.Label labelModulation;
        public System.Windows.Forms.Label labelBandwidth;
        public System.Windows.Forms.Label labelSatPC32Status;
        private System.Windows.Forms.Label labelDescSatnogsID;
        public System.Windows.Forms.Label labelSatNogsID;
        private System.Windows.Forms.GroupBox groupBox1;
        public System.Windows.Forms.CheckBox checkBoxRecordAF;
    }
}
