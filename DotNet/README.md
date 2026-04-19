# Gatebox.Variant .NET workspace

This workspace is for developing the Unity package's Unity-independent runtime
code with normal .NET tools.

The projects do not own the runtime source files. They link the files from:

```text
../Packages/Gatebox.Variant/Runtime
../Packages/Gatebox.Variant/Tests
```

Run the pure .NET tests with:

```powershell
dotnet test .\DotNet\Gatebox.Variant.DotNet.slnx
```

Unity remains the source layout used for package distribution. This workspace is
only a faster edit/build/test loop for code that should not depend on Unity APIs.
