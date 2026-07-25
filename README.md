# Medical Portal — Multi-Role Hospital Management System

A desktop application that connects reception, doctors, nurses, and pharmacy into a single hospital workflow — from patient registration to prescription to medication delivery — with each role seeing only what's relevant to them.

## 📌 Overview

Hospitals run on handoffs: reception registers a patient, a doctor prescribes, a nurse administers, a pharmacist fulfills. Without a shared system, that handoff happens on paper or gets lost between departments. This project is a local, role-based portal (WPF/C#) that keeps every step — registration, consultation, prescription, treatment, medication stock — in one connected system, with real-time notifications between roles (e.g. a nurse flags a concern, the doctor gets an urgent alert).


## 🎯 Features by Role

**🖥️ Admin**
- User management (add/edit/activate/deactivate accounts, reset passwords, import/export)
- Service management (hospital departments)
- Medication catalog management
- Dashboard with statistics and activity log

**👨‍⚕️ Doctor**
- Patient list and full patient file
- Consultation workflow and prescription creation
- Prescription/consultation history
- Real-time urgent notifications (e.g. from nursing staff), with reply

**👩‍⚕️ Nurse**
- Assigned patient list, room assignment
- Send remarks/alerts directly to the doctor
- Take charge of a patient, administer prescribed treatment
- Medication requests to pharmacy, notifications, history

**💊 Pharmacy**
- Prepare and track medication orders
- Stock management (quantities, thresholds, expiration)
- Incoming order tracking

**🏥 Reception**
- Patient registration
- Patient list/lookup

## 🛠️ Tech Stack

- **Framework:** WPF (.NET 8, C#)
- **IDE:** Visual Studio
- **Data storage:** Local JSON files (no external database — see `Database/DatabaseHelper.cs`)
- **Architecture:** Single desktop app, role-based views (`Windows/`), reusable dialogs (`Dialogs/`)

## 📁 Project Structure

```
MedicalSystem/
├── Windows/          → One window per role (Login, Admin, Doctor, Nurse, Pharmacy, Reception)
├── Dialogs/           → Reusable dialogs (add/edit user, medication, service)
├── Models/             → Data models (Patient, Prescription, Medication, User, Service, Notification, Activity)
├── Database/          → JSON-based data access layer
├── Converters/        → WPF value converters
└── App.xaml(.cs)      → Application entry point
```

## 🔑 Test Accounts (demo data)

| Role | Username | Password |
|---|---|---|
| Admin | admin | admin123 |
| Doctor | medecin | 123456 |
| Nurse | infirmier | 123456 |
| Pharmacy | pharmacien | 123456 |
| Reception | accueil | 123456 |

> These are default demo accounts auto-generated on first run (see `DatabaseHelper.InitializeData()`), used for local testing only — not real credentials.

## ▶️ Running the Project

1. Clone the repo and open `MedicalSystem.sln` (or the `.csproj`) in Visual Studio
2. Restore NuGet packages if prompted
3. Build and run (F5) — targets `.NET 8 (Windows)`
4. On first launch, default data (users, services, sample medications) is generated automatically in a local `Data/` folder

## ⚠️ Disclaimer

This project was developed for academic purposes as part of a Master's program in Digital Engineering for Healthcare. It is not intended for real clinical or medical use — data storage, authentication, and validation are simplified for a local prototype context.

## 👩‍🎓 Authors

- **Siham Ait Taleb** — **Khadija El Kinani** 
