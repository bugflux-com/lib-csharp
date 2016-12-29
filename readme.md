# Reporting library for C#

- [Documentation](http://bugflux.com/guide/master/reporting-libraries/csharp.html)
- [Contributing](http://bugflux.com/guide/master/for-developers/contributing.html)

## Deploying to NuGet

- Download `nuget.exe`
- In project properties of Bugflux project change version in `Application -> Assembly information`
- Compile lib in **RELEASE mode**
- In `Bugflux` directory run `nuget spec` or change existing `nuspec` file:
```xml
<licenseUrl>https://opensource.org/licenses/MIT</licenseUrl>
<projectUrl>http://bugflux.com/</projectUrl>
<iconUrl>http://bugflux.com/img/icon-512.png</iconUrl>
```
- `nuget pack Bugflux.csproj -Prop Configuration=Release`
- Upload generated `nupkg` file to nuget gallery