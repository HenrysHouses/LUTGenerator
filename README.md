# LUT Texture Generator

## Features

- LUT Generator Window
    - Set and reset gradient.
    - Render .png from gradient.
    - Select Material and target texture property to preview gradient.
    - Toggle Preview.
    - Name file or get Unique Name.
    - Set Save path
    - Keep generated texture on material on .png rendering.
    - Confirmation on overwriting files

- Config
    - Stores last used settings.
    - Define resolution
    - Define search strings (implemented as propertyName.Contains(SearchString))
    - Define ignored property names (implemented as propertyName.Equals(IgnoredName))

## Setup

- Install via Package Manager → Add package via git URL: 
    - https://github.com/HenrysHouses/MultiSceneTools.git
- Alternatively, download and put the folder in your Assets
- Open the LUT Generator Window under Window>Generation>LUT

## Example Video

<video width="320" height="240" controls>
  <source src="Example.mp4" type="video/mp4">
</video>

## Author

- [Henrik Hustoft](https://www.linkedin.com/in/henrik-hustoft-2366ab220/)

## License

- GNU General Public License. Refer to the [LICENSE](./LICENSE) file
