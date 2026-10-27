using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Reflection;
using static System.Windows.Forms.LinkLabel;

namespace 美团PCBA过站上传工具;

public partial class MainView : Form
{
	private bool IsRun = false;
	private SerialComm serialComm;
	private bool IsPlay = false;
	private Mp3FileReader reader;
	private WaveOut wout;

	public MainView()
	{
		InitializeComponent();
		Load += LoadAction;
		FormClosing += CloseAction;
		RunBtn.Click += RunAction;
		SetupBtn.Click += SetupAction;
	}

	private void CloseAction(object? sender, EventArgs e)
	{
		serialComm.Dispose();
		Application.Exit();
	}

	private void LoadAction(object? sender, EventArgs e)
	{
		Text = Assembly.GetExecutingAssembly().GetName().Name + Assembly.GetExecutingAssembly().GetName().Version;
		Model.Text = Properties.Settings.Default.Model;
		Field.Text = Properties.Settings.Default.Field;
		Station.Text = Properties.Settings.Default.Station;
		Com.Text = Properties.Settings.Default.Com;
		Fixid.Text = Properties.Settings.Default.Fixid;

		serialComm = new SerialComm(Com.Text, 115200)
		{
			FrameParser = FrameParsers.Delimited("\r")
		};

		serialComm.Log += (s, msg) => WriteLog("[系统] " + msg);
		serialComm.Error += (s, ex) => WriteLog("[错误] " + ex.Message);
		serialComm.Connected += (s, e) => { ShowLog("串口连接成功"); };
		serialComm.Disconnected += (s, e) => { ShowLog("串口断开连接"); };
		serialComm.TextFrameReceived += (s, text) => DataReceived(text);
	}

	private void DataReceived(string info)
	{
		if (InvokeRequired)
		{
			BeginInvoke(new Action<string>(DataReceived), info);
			return;
		}

		ShowLog("串口接收数据：" + info);
		DataSplit(info);
	}

	private void DataSplit(string datas)
	{
		var bools = new List<bool>();
		var list = datas.Split(";").ToList();
		Invoke(() =>
		{
			for (var x = 0; x < list.Count; x++)
			{
				var data = list[x];
				var color = Color.Red;
				if (CheckStation(data))
				{
					if (UploadStation(data))
					{
						color = Color.GreenYellow;
						bools.Add(true);
					}
					else
					{
						bools.Add(false);
					}

					switch (x)
					{
						case 0:
							Code1.Text = data;
							Code1.BackColor = color;
							break;
						case 1:
							Code2.Text = data;
							Code2.BackColor = color;
							break;
						case 2:
							Code3.Text = data;
							Code3.BackColor = color;
							break;
						case 3:
							Code4.Text = data;
							Code4.BackColor = color;
							break;
						case 4:
							Code5.Text = data;
							Code5.BackColor = color;
							break;
						case 5:
							Code6.Text = data;
							Code6.BackColor = color;
							break;
						case 6:
							Code7.Text = data;
							Code7.BackColor = color;
							break;
						case 7:
							Code8.Text = data;
							Code8.BackColor = color;
							break;
					}
				}
				else
				{
					bools.Add(false);
				}
			}
		});
		if (bools.Contains(false))
		{
			ShowLog("存在部分条码过站失败，查看日志获取失败原因！");
			ShowResult(false);
		}
		else
		{
			ShowLog("全部条码过站成功！");
			ShowResult(true);
		}
	}

	private void RunAction(object? sender, EventArgs e)
	{
		try
		{
			Invoke(new Action(() =>
			{
				if (IsRun)
				{
					RunBtn.Text = "未运行";
					RunBtn.BackColor = Color.OrangeRed;
					IsRun = false;
					SetupBtn.Enabled = true;
					serialComm.Stop();
					ShowLog("串口断开连接");
				}
				else
				{
					RunBtn.Text = "运行中";
					RunBtn.BackColor = Color.GreenYellow;
					IsRun = true;
					SetupBtn.Enabled = false;
					serialComm.Start(); // 打开并自动重连
				}
			}));
		}
		catch (Exception ex)
		{
			serialComm.Stop();
			ShowLog("打开串口失败：" + ex.Message);
		}
	}

	private void SetupAction(object? sender, EventArgs e)
	{
		var setupView = new SetupView();
		var result = setupView.ShowDialog();
		if (result == DialogResult.OK)
		{
			ShowLog("保存设置成功");
			LoadAction(sender, e);
		}
		else
		{
			ShowLog("取消保存设置");
		}
	}

	private string Post(string Url, string jsonStr = "")
	{
		//创建一个HTTP请求  
		var request = (HttpWebRequest)WebRequest.Create(Url);
		//Post请求方式  
		request.Method = "POST";
		//内容类型
		request.ContentType = "application/json";
		//设置参数，并进行URL编码 
		var paraUrlCoded = jsonStr; //System.Web.HttpUtility.UrlEncode(jsonParas);   
		byte[] payload;
		//将Json字符串转化为字节  
		payload = System.Text.Encoding.UTF8.GetBytes(paraUrlCoded);
		//设置请求的ContentLength   
		request.ContentLength = payload.Length;
		//发送请求，获得请求流 
		Stream writer;
		try
		{
			writer = request.GetRequestStream(); //获取用于写入请求数据的Stream对象
		}
		catch (Exception ex)
		{
			writer = null;
			throw ex;
		}

		//将请求参数写入流
		writer.Write(payload, 0, payload.Length);
		writer.Close(); //关闭请求流
		// String strValue = "";//strValue为http响应所返回的字符流
		HttpWebResponse response;
		try
		{
			//获得响应流
			response = (HttpWebResponse)request.GetResponse();
		}
		catch (WebException ex)
		{
			response = ex.Response as HttpWebResponse;
		}

		var s = response.GetResponseStream();
		//  Stream postData = Request.InputStream;
		var sRead = new StreamReader(s);
		var postContent = sRead.ReadToEnd();
		sRead.Close();
		return postContent; //返回Json数据
	}

	private bool CheckStation(string input)
	{
		var res = false;
		var model = Properties.Settings.Default.Model;
		var fields = Properties.Settings.Default.Field;
		var station = Properties.Settings.Default.Station;
		try
		{
			var url =
				"http://192.168.8.8/box_cms_v3/mes_v1/getDutTestFlowResult.php?model={0}&dut_number_name={1}&dut_number_value={2}";
			url = string.Format(url, model, fields, input);

			var JsonStr = Post(url);
			var jArray = (JObject)JsonConvert.DeserializeObject(JsonStr);
			if ((jArray["resultCode"].ToString() == "0") & (jArray["resultMsg"].ToString() == "success"))
			{
				var stationinfo = (JObject)JsonConvert.DeserializeObject(jArray["stations"].ToString());
				if (stationinfo.ContainsKey(station))
				{
					if (stationinfo[station].ToString() != "PASS")
						res = true;
					else
						ShowLog(fields + "：" + input + "当前设备已通过 " + Properties.Settings.Default.Station +
						        " 过站测试！！！不可重复测试");
				}
				else
				{
					ShowLog(fields + "：" + input + "当前工站：" + station + "未打开！检查厂测系统设置！");
				}
			}
			else
			{
				ShowLog(fields + "：" + input + "当前设备" + jArray["resultMsg"].ToString());
			}
		}
		catch (Exception ex)
		{
			ShowLog(fields + "：" + input + "检查工站异常:" + ex.Message);
		}

		return res;
	}

	private bool UploadStation(string input)
	{
		var res = false;
		try
		{
			var url =
				"http://192.168.8.8/box_cms_v3/mes_v1/postTestDataStr.php?model={0}&dut_number_name={1}&dut_number_value={2}&station={3}&elapsed=1&is_pass=true&fix_id={4}&line_name=&retry_cnt=1&is_misdetect=false";
			var model = Properties.Settings.Default.Model;
			var fields = Properties.Settings.Default.Field;
			var station = Properties.Settings.Default.Station;
			var fixid = Properties.Settings.Default.Fixid;
			url = string.Format(url, model, fields, input, station, fixid);
			var jsonStr = Post(url);
			var jArray = (JObject)JsonConvert.DeserializeObject(jsonStr);
			if (jArray["resultCode"].ToString() == "0")
				res = true;
			else
				ShowLog(fields + ":" + input + "上传工站失败：" + jArray["resultMsg"].ToString());
		}
		catch (Exception ex)
		{
			ShowLog("上传工站异常:" + ex.Message);
		}

		return res;
	}

	private void ShowLog(string log)
	{
		Invoke(new Action(() =>
		{
			if (Logs.TextLength > 20000) Logs.Clear();
			if (Logs.Text.Length > 0)
				Logs.AppendText(Environment.NewLine + DateTime.Now.ToString("HH:mm:ss") + " " + log);
			else
				Logs.Text = DateTime.Now.ToString("HH:mm:ss") + " " + log;
			Logs.SelectionStart = Logs.Text.Length;
			Logs.ScrollToCaret();
			WriteLog(log);
		}));
	}

	private void WriteLog(string log)
	{
		var FloderPath = AppDomain.CurrentDomain.BaseDirectory;
		var FilePath = FloderPath.Replace("/", "\\") + "Logs\\";
		if (!Directory.Exists(FilePath)) Directory.CreateDirectory(FilePath);

		FilePath += DateTime.Now.ToString("yyyyMMdd") + "log.txt";
		var writer = new StreamWriter(FilePath, true, System.Text.Encoding.UTF8);
		writer.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + log);
		writer.Close();
	}

	private void ShowResult(bool result)
	{
		Invoke(() =>
		{
			if (result)
			{
				Result.Text = "PASS";
				Result.BackColor = Color.GreenYellow;
				PlayShound("Success.mp3");
			}
			else
			{
				Result.Text = "NG";
				Result.BackColor = Color.Red;
				PlayShound("Beep.mp3");
			}
		});
	}

	private void PlayShound(string filename)
	{
		if (IsPlay)
		{
			wout.Stop();
			IsPlay = true;
		}

		reader = new Mp3FileReader(filename);
		wout = new WaveOut();
		wout.Init(reader);
		wout.Play();
		IsPlay = true;
	}
}