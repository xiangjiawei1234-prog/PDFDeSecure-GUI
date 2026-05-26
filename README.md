# PDFDeSecure

PDFDeSecure is a small Windows GUI PDF unlocker based on PDFsharp. It can rebuild a PDF into a new file and drop usage restrictions such as print/copy/edit locks when the document is not password-encrypted.

This GUI-focused version is based on the open-source project [abatsakidis/PDFDeSecure](https://github.com/abatsakidis/PDFDeSecure).

## Project layout

```text
.
|-- src/PDFDeSecure/        # WinForms source project
|-- artifacts/              # Local test files and generated PDFs (ignored)
|-- dist/                   # Local published executable package (ignored)
|-- PDFDeSecure.sln
|-- README.md
```

## Build

```powershell
dotnet build PDFDeSecure.sln
```

## Publish

```powershell
dotnet publish src\PDFDeSecure\PDFDeSecure.csproj -c Release -r win-x64 --self-contained true
```

The publish output is generated under:

```text
src\PDFDeSecure\bin\windows\Release\net8.0-windows\win-x64\publish
```

`dist/` is ignored by Git because published single-file builds can exceed GitHub's normal file size limit. Attach release builds to GitHub Releases or store them with Git LFS if needed.

## Usage

Run the UI:

```powershell
dotnet run --project src\PDFDeSecure\PDFDeSecure.csproj
```

Batch mode:

```powershell
PDFDeSecure.exe <input-folder> <output-folder>
```

## Notes

- `artifacts/` contains large local test files and generated PDFs.
- `dist/`, `bin/`, `obj/`, `.vs/`, and other local build caches are ignored by `.gitignore`.
- This tool does not crack password-encrypted PDFs.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
