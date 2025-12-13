
# 🛒 Store Simulator (F#)

A functional programming–based **Store Simulator** built using **F#**, **Avalonia UI**, **SQLite**, and **JSON** persistence.  
This repository contains **two independent versions** of the project, released as **V1** and **V2**, to demonstrate system evolution.

---

## 📦 Versions Overview

### 🔹 V1 – JSON Version (`/V1`)
- No authentication
- Data persistence using **JSON files**
- Focus on **functional programming fundamentals**
- Simple store workflow (catalog, cart, checkout)

### 🔹 V2 – Database Version (`/V2`)
- **User authentication**
- **SQLite database** for persistence
- Desktop GUI using **Avalonia UI**
- Extended and production-style architecture

> 📌 Both versions are maintained in the same repository for comparison and learning purposes.

---

## 🎯 Project Purpose

This project is **educational** and designed to help learners:
- Understand **functional programming** concepts in F#
- Work with **immutable data structures**
- Design **pure and testable functions**
- Manage application state without side effects
- Integrate **SQLite** with functional code
- Build **desktop GUI applications** using Avalonia

---

## ✨ Key Features

- User Authentication (V2)
- Product Catalog Browsing
- Search & Filtering
- Inventory Management
- Shopping Cart Management
- Discount System
- Checkout & Total Calculation
- JSON Persistence (V1)
- SQLite Persistence (V2)
- Avalonia Desktop GUI (V2)

---

## 📂 Repository Structure

```text
store-simulator
│
├── V1
│   ├── src
│   ├── StoreTests
│   └── README.md
│
├── V2
│   ├── src
│   ├── StoreTests
│   └── README.md
│
├── Block Diagram.jpeg
├── .gitignore
└── README.md
```

Each version contains its own source code, tests, and documentation.

---

## 🏗️ System Architecture

Both versions follow a **modular functional architecture**:

- Catalog Module
- Cart Module
- Checkout Module
- Persistence Module
- UI Module (Avalonia – V2)
- Authentication Module (V2)

Modules communicate strictly through **function inputs and outputs**.

---

## 🧠 Functional Programming Concepts Used

- Immutability
- Pure Functions
- Pattern Matching
- Option Types
- Model–Update–View (MVU) Pattern

---

## 🧪 Testing

Unit tests cover:
- Product search
- Cart operations
- Checkout logic
- Discount handling
- Authentication (V2)

---

## ▶️ How to Run

### Run V1 (JSON Version)
```bash
cd V1/src
dotnet build
dotnet run
```

### Run V2 (Database Version)
```bash
cd V2/src/StoreSimulator.UI
dotnet build
dotnet run
```

---

## 🚀 Future Improvements

- Advanced discount rules
- User purchase history
- Admin dashboard
- Inventory analytics
- Web-based frontend

---

## 📌 Conclusion

This repository demonstrates how **functional programming principles** can be applied to real-world applications in F#, showing a clear evolution from a simple JSON-based system (V1) to a full database-backed desktop application (V2).
