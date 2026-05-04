# Gatebox.Variant

Gatebox.Variant は、Unity 上で JSON を「C# のクラスにマッピングする前提のデータ」ではなく、「JSON のまま扱うデータ」として操作するためのライブラリです。

API レスポンス、設定ファイル、LLM から返ってくる可変な JSON、外部サービスごとに少しずつ形が違うデータなど、毎回 DTO を定義するほどではないけれど、文字列や `Dictionary<string, object>` で触るにはつらい場面を想定しています。

`JVariant` / `JObject` / `JArray` を使うと、JSON の構造を保ったまま、必要な場所だけを取り出したり、編集したり、また JSON として書き戻したりできます。

## 特徴

- JSON の値を `JVariant` として扱える
- オブジェクトは `JObject`、配列は `JArray` として自然に編集できる
- `obj["name"]` や `array[0]` のようなアクセスができる
- `AsInt()` / `AsString()` / `AsBool()` などで必要な型として取り出せる
- `RequireString()` など、期待した型でないときに例外にする API もある
- JSON 文字列と UTF-8 JSON のパースに対応
- `ToJson()` / `ToU8Json()` で JSON として出力できる
- `JsonFormatPolicy` で一行出力、整形出力、特殊な浮動小数点値の扱いなどを指定できる
- 必要であれば C# の型との相互変換もできる

## どういうときに向いているか

Gatebox.Variant は、スキーマが安定していて C# の型としてきっちり扱いたいデータよりも、次のような JSON に向いています。

- フィールドが増減しやすい API レスポンス
- ユーザーや外部サービスが作る設定 JSON
- 一部だけ読めればよい大きめの JSON
- LLM やスクリプトから返る、形が少し揺れる JSON
- Unity 内で JSON を一時的に編集して渡したい場面

JSON を JSON のまま扱いたいときに、文字列操作より安全で、DTO を作るより軽く使える場所を目指しています。

# Unity への導入

Unity プロジェクトに導入する場合は、Package Manager の Install Package from Git URL で以下を指定してください。
```
https://github.com/gateboxlab/Gatebox.Variant.git?path=/Packages/Gatebox.Variant
```

または `Packages/Gatebox.Variant` をプロジェクトの `Packages` 以下に配置してください。

# 基本的な使い方

```csharp
using Gatebox.Variant;

var json = @"
{
  ""name"": ""Gatebox"",
  ""count"": 3,
  ""enabled"": true,
  ""items"": [
    { ""id"": 1, ""label"": ""first"" },
    { ""id"": 2, ""label"": ""second"" }
  ]
}";

JVariant root = new JVariant().Parse(json, throws: true);

string name = root["name"].AsString();
int count = root["count"].AsInt();
bool enabled = root["enabled"].AsBool();
string secondLabel = root["items"][1]["label"].AsString();
```

`JVariant` のインデクサは読み取り用です。存在しないキーや範囲外の要素を読んだ場合は、空の `JVariant`、つまり null 相当の値を返します。

```csharp
var missing = root["missing"];

if (missing.IsNull())
{
    // キーが存在しない、または null として扱える
}
```

## JSON を組み立てる

`JObject` と `JArray` は JSON のオブジェクトと配列を表します。初期化子を使ってそのまま JSON らしく書けます。

```csharp
using Gatebox.Variant;

var obj = new JObject
{
    ["type"] = "message",
    ["priority"] = 2,
    ["active"] = true,
    ["tags"] = new JArray { "unity", "json", "variant" },
    ["payload"] = new JObject
    {
        ["text"] = "hello",
        ["score"] = 0.95,
    },
};

obj.Set("updated", true);

string json = obj.ToJson();
```

`JObject` / `JArray` のインデクサは、存在しない要素へのアクセスで内容を作ることがあります。読み取りだけをしたい場合は `Get()` を使うと、構造を変更せずに値を取得できます。

```csharp
JVariant value = obj.Get("optional");
```

## パース

```csharp
JVariant value = new JVariant().Parse("{\"value\":123}", throws: true);
```

UTF-8 の入力も扱えます。

```csharp
U8View bytes = U8View.Create("{\"value\":123}");
JVariant value = new JVariant().Parse(bytes, throws: true);
```

パースに失敗した場合、`throws: false` では null 相当の `JVariant` を返します。失敗を明確に扱いたい場合は `throws: true` を指定してください。

```csharp
JVariant maybeNull = new JVariant().Parse("{", throws: false);

try
{
    JVariant strict = new JVariant().Parse("{", throws: true);
}
catch (JsonParseException)
{
    // invalid JSON
}
```

パーサーは実用上困りにくいよう、いくつかの緩い入力も受け入れます。たとえばコメント、末尾カンマ、引用符なしの単純なキーなどです。ただし、これらは厳密な JSON ではないため、外部との互換性が必要なデータでは標準的な JSON を使うことをおすすめします。

## 値の取り出し

`AsXxx()` は、多少変換しながら値を取り出します。

```csharp
int n = value["count"].AsInt();
double rate = value["rate"].AsDouble();
string text = value["text"].AsString();
bool ok = value["ok"].AsBool();
```

期待した型であることを保証したい場合は `RequireXxx()` を使います。

```csharp
string id = value["id"].RequireString();
JArray items = value["items"].RequireArray();
JObject body = value["body"].RequireObject();
```

任意の C# 型へ変換することもできます。

```csharp
var numbers = value["numbers"].As<List<int>>();
var table = value["settings"].As<Dictionary<string, string>>();
```

逆に、C# の値から `JVariant` を作ることもできます。

```csharp
JVariant number = JVariant.Create(123);
JVariant array = JVariant.Create(new[] { 1, 2, 3 });
```

## JSON として出力する

```csharp
string pretty = value.ToJson();
string oneLine = value.ToJson(JsonFormatPolicy.OneLiner);
string formatted = value.ToJson(JsonFormatPolicy.Pretty);
U8View utf8 = value.ToU8Json(JsonFormatPolicy.Mixed);
```

`ToString()` はデバッグ向けの簡易表現です。JSON として出力したい場合は `ToJson()` または `Stringify()` を使ってください。



## テスト

Unity Test Framework 用のテストは `Packages/Gatebox.Variant/Tests` にあります。
こちらは Unity 上でテストを実行できます。Unity Test Runner から実行してください。

また Unity なしで VisualStudio 上でのテストプロジェクトも含まれており、次のように実行できます。

```powershell
dotnet test .\DotNet\Gatebox.Variant.DotNet.slnx --no-restore
```

# 設計

## JValue

JSON の値は参照型である `JValue` によって表現され、`JValue` に対する View として `JVariant` が存在します。
主に利用する型は `JVariant` になりますが、その内部には `JValue` があることを理解しておくとより快適に利用できます。

`JValue` 以下の値の union のような存在になっています。

- null
- bool
- long
- double
- string
- JSON のオブジェクト
- JSON 配列

`JValue` は各種のプリミティブから暗黙に生成されます。
この時数値の内部表現は long であるため、ulong 入れると精度が落ちることがあるので注意してください。

```csharp
JValue i = 1;
JValue b = true;
JValue s = "string";
JValue d = 1.5;
JValue obj = new JObject();
JValue array = new JArray();
```

これらの暗黙の変換があるため「JValue を引数として受けるメソッド」は自由に様々な値を指定できる反面、予期しない変換が行われてしまうことがあるので注意してください。

### Null

引数なしコンストラクタで生成した `JValue` は JSON の `null` を示します。
`JValue` 自体が参照型であり、「null」と「null を示す JValue」がそれぞれ存在することに注意してください。この使い分けを避けるために可能な限り `JVariant` を利用することをおすすめします。

## JVariant 

`JVariant` は `JValue` のみを内部に持ち、`JValue` に対する変更不能な View としての役割を持ちます。
また、Gatebox.Variant の多くの機能は `JVariant` を起点に利用できるようなインターフェースになっています。

`JVariant` は値型(struct) です。内部に `JValue` を持ちますが、その `JValue` が 「null」 である状態と、「null を示す JValue」である状態をできる限り同一視するように設計しています。`JValue` は `JVariant` に入れて扱うことで、null の問題を回避できます。

### インデクサ

`JVariant` のインデクサはキーとして string, int の両方を受け取ることができ、内部の状態とミスマッチがあった場合は null を指す `JVariant` を返し、例外を投げません。

```csharp
JVariant obj = new JObject()
{
  ["Key"] = "Value", 
};

// 内部は Object であっても int のインデクサを受けられる。単純に「見つからない」と考える。
var v1 = obj["Key"]; // => "Value"
var v2 = obj[1];     // => null

JVariant array = new JArray() 
{ 
  0, 1, 2
};

// 配列に対する string も同様
var v3 = array["Key"]; // => null
var v4 = array[1];     // => 1
```

`JVariant` は変更不能な Viewであり、インデクサは読み取り専用です。変更したい場合は `JObject`, `JArray` を経由して行ってください。

```csharp
JVariant variant = new JObject()
{
  ["object"] = new JObject(),
  ["array"] = new JArray(),
};

// これはできない
// variant["object"]["new_key"] = 1;

// こうする
JObject obj1 = variant["object"].RequireObject();
obj1["new_key"] = 1;

JArray ary1 = variant["array"].RequireArray();
ary1.Add(1);
```

## 配列

JSON の配列の表現には `JArray` を利用します。`JArray` は `IList<JValue>` を継承しており、通常の List として利用できます。また、`JValue` は各種のプリミティブから暗黙に生成されるため「いろんなものが入る配列」になります。

`JArray` は値型(struct) です。内部的にはListオブジェクトを持っていますが、null であることと要素 0 配列であることを区別せず利用できるようにしています。


### インデクサ

`JArray` のインデクサは `JValue` であり、get, set の両者を持ちます。また、存在しないインデックスのアクセスは、指定されたインデックスまで配列を広げてその要素を返却します。例外は投げません。

```csharp
JArray array = new JArray();

// これは例外を投げない、10 まで範囲が広がり、その要素(null)が返却される。
var item = array[10]; 
```

この挙動は手軽に「半定型」のような JSON データを構築する際に最も簡潔記述することを優先した仕様です。反面、勝手に広がってしまう点を見ると危険でもあるため、そのような懸念がある場合は `Get(int)` を利用してください。

## オブジェクト

JSON のオブジェクトの表現には `JObject` を利用します。`JObject` は `IDictionary<string, JValue>` を継承しており、通常の Dictionary として利用できます。`JValue` は各種のプリミティブから暗黙に生成されるため「いろんなものが入る辞書」になります。

`JObject` は値型(struct) です。内部的にはオブジェクトを持っていますが、null であることと要素を持たないオブジェクトであることを区別せず利用できるようにしています。

`JObject` の要素の挿入順は管理されません。Dictionary としてのインターフェースしか持たず、JSONの出力時の順番も制御できません。

## JVariant 互換型

Gatebox.Variant は、いわゆる DTO をシリアライズ・デシリアライズするスタイルを志向していません。
データ型とリフレクションで相互変換する機構を持ちますが、あくまでそれは簡潔にコードを書くためのユーティリティであると位置づけています。

Gatebox.Variant と相互運用する型は `IVariantConvertible` を実装し、`JVariant` を受けるコンストラクタを定義してください。

```csharp
class MyData : IVariantConvertible
{
  private int value;

  public MyData(JVariant v)
  {
    this.value = v["value"].AsInt();
  }

  public JVariant AsVariant()
  {
    // JSON 表現を返す。
    return new JObject(){
      ["value"] = this.value,
    };
  }
}
```
これにより `As<MyData>()` で `JVariant` から変換でき、`AsVariant()` で `JVariant` へ変換できるようになります。

このようなスタイルを取ることにより、JSON の一部分を柔軟に必要な型に変換するようなコードが素直に書けるようになります。

```csharp

// このAPIは "type" にだいたいの状態、 "payload" に状況によって違う構造の値を返してくる
JVariant api_result = await GetDataFromWebApi();

// payload を type に合わせて解釈し、加工して返す…みたいなコード
if ( api_result["type"].AsString() == "data" )
{
  var data = api_result["payload"].As<MyData>();
  return new JObject(){
    ["status"] = "ok",
    ["data"] = data.AsVariant(),
  };
}
if ( api_result["type"].AsString() == "summary" )
{
  var summary = api_result["payload"].As<Summary>();
  return new JObject(){
    ["status"] = "ok",
    ["summary"] = summary.AsVariant(),
  };
}
```



