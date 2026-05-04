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

## どういうときに向いているか

Gatebox.Variant は、スキーマが安定していて C# の型としてきっちり扱いたいデータよりも、次のような JSON に向いています。

- フィールドが増減しやすい API レスポンス
- ユーザーや外部サービスが作る設定 JSON
- 一部だけ読めればよい大きめの JSON
- LLM やスクリプトから返る、形が少し揺れる JSON
- Unity 内で JSON を一時的に編集して渡したい場面

JSON を JSON のまま扱いたいときに、文字列操作より安全で、DTO を作るより軽く使える場所を目指しています。
