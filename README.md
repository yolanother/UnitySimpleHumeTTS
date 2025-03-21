# UnitySimpleHumeTTS

A simple Unity library for requesting TTS via Hume.

## Description

UnitySimpleHumeTTS is a Unity package that allows developers to easily integrate Text-to-Speech (TTS) functionality into their Unity projects using the Hume service.

![image](https://github.com/user-attachments/assets/36dfc3b2-a1a5-4c8f-8845-e3d060d8af48)


## Features

- Easy integration with Unity
- Simple API for requesting TTS
- Supports multiple voices and languages

## Installation

### From GitHub

1. Clone the repository from GitHub:
   ```sh
   git clone https://github.com/yolanother/UnitySimpleHumeTTS.git
   ```

2. Open your Unity project.

3. In Unity, go to `Window > Package Manager`.

4. Click the `+` button and select `Add package from disk...`.

5. Navigate to the cloned repository and select the `package.json` file.

6. Click `Open` to add the package to your project.

### Using Unity Package Manager

1. Open your Unity project.

2. In Unity, go to `Window > Package Manager`.

3. Click the `+` button and select `Add package from git URL...`.

4. Enter the GitHub repository URL:
   ```
   git clone https://github.com/yolanother/UnitySimpleHumeTTS.git
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
