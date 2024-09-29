# CM3D2.ReadAsShiftJisFix.Patcher
A late for 9 years plugin, that let you no longer need to change the system language or use a language simulator to play CM3D2.

<br>

# This plugin is not working at the moment, please help

Using this plugin will allow you to enter the game, but you will get a runtime error when you load the save file.

I tried all the solutions I could think of but none of them worked.

If you are interested, please try to fix it, it's WTFPL.



![b1568f79a55de02bb89008f431a384fd](https://github.com/user-attachments/assets/e6789e21-44ef-4639-96be-ee4e7e1a9dbf)




<br>
<br>



## Usage

you should have [Sybaris](https://seesaawiki.jp/cm3d2-mod/d/%a5%d7%a5%e9%a5%b0%a5%a4%a5%f3/%a4%b7%a4%d0%a4%ea%a4%b9) installed.

1. Put mono 2.0's `I18N.dll` and `I18N.CJK.dll` in `CM3D2\CM3D2x64_Data\Managed` 
2. Put CM3D2.1_ReadAsShiftJisFix.Patcher.dll in `CM3D2\Sybaris\Loader`
3. Launch the game

<br>

## Additional

If you don't trust the dll I provide
1. For this plugin, you can download it from Github Action
2. For `I18N.dll` and `I18N.CJK.dll` you can download mono from https://download.mono-project.com/archive/2.0/windows-installer/5/index.html and install it, then you can find `I18N.dll` and `I18N.CJK.dll` in Mono-2.0\lib\mono\2.0

Tested in 
- CM3D2 1.72.0 + Sybaris ver160930
- CM3D2CBL 1.72.0 + Sybaris ver160930

<br>

## Story

As we all know, CM3D2 requires the Windows system language to be set to Japanese (encoded as Shift-JIS) in order to enter the game normally.

If your Windows system language is Simplefied Chinese or ANSI character encoding you will receive:
```
NAssert! Yotogi.Category
enum parse error.[堹梸]
 
Exception: Yotogi.Category
enum parse error.[堹梸]
  at NDebug.Assert (System.String message) [0x00000] in <filename unknown>:0 
  at Yotogi+SkillData..ctor (.CsvParser csv, .CsvParser csv_acq, Int32 y, System.Collections.Generic.Dictionary2 command_data_cell_dic) [0x00000] in <filename unknown>:0 
  at Yotogi.CreateYotogiData () [0x00000] in <filename unknown>:0 
  at GameMain.OnInitialize () [0x00000] in <filename unknown>:0 
  at MonoSingleton1[GameMain].Initialize (.GameMain instance) [0x00000] in <filename unknown>:0 
  at MonoSingleton1[T].Awake () [0x00000] in <filename unknown>:0 
```

`堹梸` is generated when trying to decode `淫欲` encoded in Shift-JIS using GBK encoding.


I always thought KISS did this on purpose like ILLUSION did, but after checking the game's internal code, I found that this was not the case.

You see in `Assembly-CSharp-firstpass.dll -> Nuty.cs` there is a method:
```
	public static string ReadAsShiftJis(byte[] bArray)
	{
		StringBuilder stringBuilder = new StringBuilder(bArray.Length + 1);
		NUty.MultiByteToWideChar(1U, 0U, bArray, -1, stringBuilder, stringBuilder.Capacity);
		return stringBuilder.ToString();
	}
```
`1U` mean System default encoding, so when your system language is not Japanese, you will get wrong results.


In `Assembly-CSharp.dll -> Yotogi.cs` there is a class

it called GetCellAsString() and the GetCellAsString() return ReadAsShiftJis(), then an ArgumentException occurs, and you get this error message.

```
	public class SkillData
	{
		public SkillData(CsvParser csv, CsvParser csv_acq, int y, Dictionary<int, int[]> command_data_cell_dic)
		{
			int num = 0;
			this.id = csv.GetCellAsInteger(num++, y);
			num++;
			string text = csv.GetCellAsString(num++, y);
			try
			{
				this.category = (Yotogi.Category)((int)Enum.Parse(typeof(Yotogi.Category), text));
			}
			catch (ArgumentException)
			{
				NDebug.Assert("Yotogi.Category\nenum parse error.[" + text + "]");
			}
```
`Assembly-CSharp.dll -> CsvParser.cs` :
```
	public unsafe override string GetCellAsString(int cell_x, int cell_y)
	{
		if (this.class_ptr_ == IntPtr.Zero)
		{
			return string.Empty;
		}
		int num = this.dll_csv_parser_.GetDataSizeString(this.class_ptr_, cell_x, cell_y);
		if (num <= 0)
		{
			return string.Empty;
		}
		byte[] array = new byte[num];
		fixed (byte* ptr = (ref array != null && array.Length != 0 ? ref array[0] : ref *null))
		{
			this.dll_csv_parser_.GetCellAsString(this.class_ptr_, cell_x, cell_y, (IntPtr)((void*)ptr), num);
		}
		return NUty.ReadAsShiftJis(array);
	}
```


To sum up, this is just a code bug, setting the decoding option to the default code page.



So I just used IL operation to change 1U to 932(Shift-JIS), but it didn’t work.

After I tried other methods, an error should have been thrown:
```
NotSupportedException: CodePage 932 not supported
at System.Text.Encoding.GetEncoding (Int32 codepage) [0x00000] in <filename unknown>:0
at NUty.ReadAsShiftJis (System.Byte[] bArray) [0x00000] in <filename unknown>:0
at CsvParser.GetCellAsString (Int32 cell_x, Int32 cell_y) [0x00000] in <filename unknown>:0
at Yotogi.CreateYotogiData () [0x00000] in <filename unknown>:0
at GameMain.OnInitialize () [0x00000] in <filename unknown>:0
at MonoSingleton`1[GameMain].Initialize (.GameMain instance) [0x00000] in <filename unknown>:0 at MonoSingleton`1[T].Awake () [0x00000] in <filename unknown>:0
```

Wow, 932 is not supported, but the default encoding can be used?

But anyway, adding `I18N.dll` and `I18N.CJK.dll` can solve this problem.

So this is the plugin that came 9 years late.




