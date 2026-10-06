# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

The Robocopy GUI is a Windows Forms application that provides a user-friendly interface for the Robocopy command-line utility. It allows users to configure and execute file copy operations with various options and exclusions.

## Key Components

1. **MainForm.cs**: The main form of the application that handles the UI and user interactions.
2. **RobocopyRunner.cs**: Handles the execution of Robocopy commands and parsing of output.
3. **AppConfig.cs**: Configuration class to store user preferences.
4. **Custom Controls**: RoundedTextBox, RoundedButton, and RoundedPanel for modern UI elements.

## Development Commands

### Building the Application

To build the application, use the following command in the project directory:

```
dotnet build
```

### Running the Application

To run the application, use:

```
dotnet run
```

### Running Tests

The project includes unit tests in the Tests directory. To run all tests:

```
dotnet test
```

To run a specific test:

```
dotnet test --filter "FullyQualifiedName~RobocopyGui.Tests.<TestClassName>.<TestMethodName>"
```

## Code Architecture

The application follows a modular architecture with clear separation of concerns:

1. **UI Layer**: MainForm.cs handles all user interface elements and interactions.
2. **Business Logic Layer**: RobocopyRunner.cs manages the execution of Robocopy commands and processing of output.
3. **Data Layer**: AppConfig.cs stores and manages user preferences and configuration.
4. **Custom Controls**: Provides modern UI elements with rounded corners and custom styling.

## Development Guidelines

1. Follow the engineering principles outlined in AGENTS.md when making changes.
2. Maintain consistent styling with the custom controls for all UI elements.
3. Ensure proper error handling and logging in RobocopyRunner.cs.
4. Update tests when modifying functionality in the main codebase.

## Additional Notes

- The application uses WinForms for the UI.
- RobocopyRunner.cs implements progress tracking and event handling for real-time feedback.
- Custom controls are used to achieve a modern, iOS-inspired look.

This CLAUDE.md file provides guidance for working with the Robocopy GUI project, helping future instances of Claude Code to understand the project structure and development workflow.