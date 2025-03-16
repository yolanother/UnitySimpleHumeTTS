# UnitySimpleHumeTTS

A simple Unity library for requesting TTS via Hume.

## Description

UnitySimpleHumeTTS is a Unity package that allows developers to easily integrate Text-to-Speech (TTS) functionality into their Unity projects using the Hume service.

## Features

- Easy integration with Unity
- Simple API for requesting TTS
- Supports multiple voices and languages

## Installation

### From GitHub

1. Clone the repository from GitHub:
   ```sh
   git clone https://github.com/yourusername/UnitySimpleHumeTTS.git
   ```

2. Open your Unity project.

3. In Unity, go to `Assets > Import Package > Custom Package...`.

4. Select the `UnitySimpleHumeTTS.unitypackage` file from the cloned repository.

5. Click `Import` to add the package to your project.

### Using Unity Package Manager

1. Open your Unity project.

2. In Unity, go to `Window > Package Manager`.

3. Click the `+` button and select `Add package from git URL...`.

4. Enter the GitHub repository URL:
   ```
   https://github.com/yourusername/UnitySimpleHumeTTS.git
   ```

5. Click `Add` to install the package.

## Usage

1. Add the `HumeClient` component to a GameObject in your scene.

2. Configure the `HumeClient` component with your Hume API key.

3. Use the `HumeClient` API to request TTS:
   ```csharp
   using UnitySimpleHumeTTS;

   public class Example : MonoBehaviour
   {
       private HumeClient humeClient;

       void Start()
       {
           humeClient = GetComponent<HumeClient>();
           humeClient.Speak("Hello, world!");
       }
   }
   ```

## Contributing

Contributions are welcome! Please open an issue or submit a pull request on GitHub.

## License

This project is licensed under the MIT License.

## Acknowledgements

This package was mostly AI built using Roo Code.
