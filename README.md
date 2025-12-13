# 🛒 Store Simulator (F#)

A functional programming–based **Store Simulator** built using **F#**, **Avalonia UI**, and **SQLite**.  
This project simulates the core workflow of an online store while emphasizing **immutability**, **pure functions**, and **type-safe design**.

---

## 📌 Project Overview

The Store Simulator provides:
- User registration and authentication
- Product catalog browsing and searching
- Shopping cart management
- Stock validation and discount handling
- Checkout and total price calculation

The application includes a **desktop GUI** built with **Avalonia**.  
Data persistence is handled using:
- **SQLite** for structured data (users, products, carts, inventory)
- **JSON files** for storing checkout summaries

This project demonstrates how **functional programming principles** can be applied to a realistic application.

---

## 🎯 Purpose

This project is primarily **educational** and aims to help learners:
- Understand functional programming concepts in **F#**
- Work with **immutable data structures**
- Design **pure and testable functions**
- Manage application state without side effects
- Integrate **SQLite** with functional code
- Build desktop GUI applications using **Avalonia**

---

## ✨ Key Features

- **User Authentication**
- **Product Catalog**
- **Search & Filtering**
- **Inventory Management**
- **Cart Management**
- **Discount System**
- **Checkout & Total Calculation**
- **SQLite + JSON Persistence**
- **Avalonia Desktop GUI**

---

## 📂 Project Structure

```text
store-simulator
│
├── .gitignore
├── Block Diagram.jpeg
│
├── src
│   ├── StoreApp.fsproj
│   ├── Program.fs
│   ├── Catalog.fs
│   ├── Cart.fs
│   ├── Cartjson.fs
│   ├── PriceCalc.fs
│   ├── BackupManager.fs
│   ├── cart.json
│   │
│   └── StoreSimulator.UI
│       ├── StoreSimulator.UI.fsproj
│       ├── Program.fs
│       ├── App.axaml
│       ├── App.axaml.fs
│       ├── ViewLocator.fs
│       ├── cart.json
│       │
│       ├── Assets
│       │   └── avalonia-logo.ico
│       │
│       ├── Views
│       │   ├── MainWindow.axaml
│       │   └── MainWindow.axaml.fs
│       │
│       ├── ViewModels
│       │   ├── MainWindowViewModel.fs
│       │   └── ViewModelBase.fs
│       │
│       └── backups
│
├── StoreTests
│   ├── StoreTests.fsproj
│   ├── Tests.fs
│   └── TestResults
│
└── README.md
```

---

## 🏗️ System Architecture

The application follows a **modular functional architecture**:
- Authentication Module
- Catalog Module
- Cart Module
- Checkout Module
- Persistence Module
- UI Module (Avalonia)

Modules communicate only through function inputs and outputs.

---

## 🧠 Functional Programming Concepts Used

- Immutability
- Pure Functions
- Pattern Matching
- Option Types
- Model–Update–View Pattern

---

## 🧪 Testing

Unit tests cover:
- Authentication
- Product search
- Cart operations
- Checkout and discounts
- Error handling

---

## ▶️ How to Run

```bash
cd src/StoreSimulator.UI
dotnet build
dotnet run
```

---

## 🚀 Future Improvements

- Advanced discounts
- User roles and history
- Inventory reporting
- Web deployment

---

## 📌 Conclusion

This project demonstrates applying functional programming principles to a real-world F# application using Avalonia and SQLite.
