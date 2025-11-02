# Anytime-Anywhere-NAS

**Turn your existing Windows or Linux laptop into a simple, non-destructive NAS with one click.**

This is a desktop application built with C# and Avalonia UI that makes it incredibly easy to start a network share from your everyday computer without wiping your OS or buying new hardware.

## What is this?

Ever wanted a simple Network Attached Storage (NAS) for your home network but didn't want to dedicate a whole computer or buy a new device? This app is the solution.

It's a "control panel" that lets you take any folder on your laptop and safely share it with your entire network. It's perfect for:
* Backing up files from your phone.
* Creating a central media folder for smart TVs.
* Sharing project files between your computers.

## How it Works (The Magic) ??

This app **does not** erase your operating system. Instead, it uses **Docker** to run a lightweight, isolated Linux container that manages your file sharing (Samba) service.

* **On Windows:** The app automatically leverages **WSL 2 (Windows Subsystem for Linux)** to run the Docker container. This gives you a high-performance Linux NAS running *inside* your Windows machine without any complex setup.
* **On Linux:** The app uses the native Docker runtime for a lightweight, efficient server.

Your app is just a user-friendly GUI that automatically configures and manages this container for you.

## Key Features

* **Non-Destructive:** Does not format drives or uninstall your OS. It just shares a folder you pick.
* **Cross-Platform:** A single C# codebase for **Windows** and **Linux** desktops, built with Avalonia UI.
* **Smart Setup:** Automatically scans your system specs (RAM, CPU) to recommend a "NAS Profile" and allocates resources safely.
* **Safe Management:** Gracefully stop and resume your NAS service at any time without data corruption.
* **Resilient:** Automatically restarts on system boot and is protected from sudden power outages by your laptop's built-in battery.
* **Flexible:** Easily change which folder you are sharing or update resource limits from the app.

## Project Status

**This project is currently in active development.**

## Contributing

This is an open-source project, and contributions are welcome! If you'd like to help, please feel free to fork the repository and submit a Pull Request.