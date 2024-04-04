using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Vintagestory.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;

class Debug {

	public static ICoreAPI api;

	public static void Log(string aLog) {
		//api.Logger.Debug("Debug.Log ="+aLog);
	}

}



// TODO: Udělat Libs pro Unity
class JsonUtility {
	public static string ToJson(object aData) {
		return JsonSerializer.Serialize(aData);
	}
}

/*
EN: Exclusive ownership of this file cannot be claimed by persons other than the authors of the file.
CZ: Na tento soubor nelze uplatni výhradní vlastnicví, jiným osobám než autorm souboru.

LICENSE - MIT

Copyright (c) 2023 Hotárek Lukáš

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
*/