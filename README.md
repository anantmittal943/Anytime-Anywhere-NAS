# Anytime-Anywhere-NAS

**Turn your existing Windows or Linux laptop into a simple, non-destructive NAS with one click.**

This is a desktop application built with C# and Avalonia UI that makes it incredibly easy to start a network share from your everyday computer without wiping your OS or buying new hardware.

## What is this?

Ever wanted a simple Network Attached Storage (NAS) for your home network but didn't want to dedicate a whole computer or buy a new device? This app is the solution.

It's a "control panel" that lets you take any folder on your laptop and safely share it with your entire network. It's perfect for:
* Backing up files from your phone.
* Creating a central media folder for smart TVs.
* Sharing project files between your computers.

## How it Works (The Magic)

This app **does not** erase your operating system. Instead, it uses **Docker** to run a lightweight, isolated Linux container that manages your file sharing (Samba) service.

* **On Windows:** The app automatically leverages **WSL 2 (Windows Subsystem for Linux)** to run the Docker container. This gives you a high-performance Linux NAS running *inside* your Windows machine without any complex setup.
* **On Linux:** The app uses the native Docker runtime for a lightweight, efficient server.

Your app is just a user-friendly GUI that automatically configures and manages this container for you.

## Key Features

* **Non-Destructive:** Does not format drives or uninstall your OS. It just shares a folder you pick.
* **Cross-Platform:** A single C# codebase for **Windows** and **Linux** desktops, built with Avalonia UI.
* **One-Click Install:** On Windows, the app can install Docker for you if it's not detected.
* **Smart Resource Management:** Safely allocates a small portion of your CPU and RAM to the NAS.
* **Remembers Your Settings:** Automatically saves and reloads your last used folder path.
* **Resilient:** Your laptop's built-in battery acts as a free Uninterruptible Power Supply (UPS).

## Getting Started

### Prerequisites

#### Windows (10 or 11)
1.  **.NET 8 Desktop Runtime**: [Download and install the x64 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime?os=windows&arch=x64).
2.  **Docker Desktop**: The app can attempt to install this for you. However, for a smoother experience, it's recommended to [install it manually from the official website](https://www.docker.com/products/docker-desktop/).
    *   Ensure Docker Desktop is running before you start the app.

#### Linux
1.  **.NET 8 Runtime**: Follow the official Microsoft instructions to [install the .NET runtime for your distribution](https://learn.microsoft.com/en-us/dotnet/core/install/linux).
2.  **Docker Engine**: Install Docker for your distribution. You can find instructions on the [official Docker website](https://docs.docker.com/engine/install/).
    *   After installing, you must add your user to the `docker` group to avoid permission errors:
        ```bash
        sudo usermod -aG docker $USER
        ```
    *   **Important**: You must log out and log back in for this change to take effect.

### Installation
1.  Go to the [**Releases** page](https://github.com/anantmittal943/Anytime-Anywhere-NAS/releases) of this repository.
2.  Download the latest version for your operating system.
3.  Unzip the folder and run the `Anytime-Anywhere-NAS` executable.

## How to Use
1.  **Launch the application.** It will immediately check your system for Docker.
2.  **Click "Select Folder"** to choose the folder you want to share on your network.
3.  **Click "Start NAS".** The app will configure and start the Samba container.
4.  **Access your share!** On another computer on the same network, open File Explorer (Windows) or your file manager (Linux/macOS) and go to:
    ```
    \\<YOUR_LAPTOP_IP_ADDRESS>\MyNasShare
    ```
    You can find your laptop's local IP address in your system's network settings.

## Troubleshooting

### Windows Keeps Asking for a Password
This application configures the share for guest access, but modern versions of Windows disable insecure guest logons by default for security reasons.

If you are prompted for a password, you can either:
1.  Enter any username (e.g., "guest") with no password.
2.  If that fails, you may need to enable insecure guest logons on the **client machine** (the one trying to access the share).
    *   Press `Win + R`, type `gpedit.msc`, and press Enter.
    *   Navigate to: `Computer Configuration > Administrative Templates > Network > Lanman Workstation`.
    *   Find the "Enable insecure guest logons" policy, double-click it, select **Enabled**, and click OK.

### "Docker permission denied" on Linux
If you see a status message about permission being denied, it means your user account is not part of the `docker` group.
*   Run `sudo usermod -aG docker $USER` in a terminal.
*   **Log out and log back in.** This step is mandatory.
*   Restart the application.

## Project Status

**This project is currently in active development.**

## Contributing

This is an open-source project, and contributions are welcome! If you'd like to help, please feel free to fork the repository and submit a Pull Request.