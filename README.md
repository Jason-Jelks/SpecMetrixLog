# SpecMetrixLog

**SpecMetrix Logging System** written in **.NET 8**, designed to accept event messages/logs from SpecMetrix and store them in **MongoDB**.  
This logging system is built for **high-speed event processing**, **deduplication**, and **efficient storage**.

---

## **🔹 Features & Enhancements**
### ✅ **Core Functionalities**
- **Receives logs from SpecMetrix** and writes to MongoDB.
- **Deduplication filter** to prevent duplicate high-speed logs from flooding the system.
- **Caching system** for **fast retrieval** of recent events.
- **Automated MongoDB collection setup** for optimal performance.

### 🔥 **New Features & Improvements**
- **🌎 Cross-Platform Support**:  
  - Optimized for **Windows/Linux/macOS**.  
  - Runs as a **Windows Service** if deployed on Windows.  
- **🛠 Configuration Handling**:  
  - Loads settings from `C:\Configurations\specmetrix.json`.  
  - **Automated MongoDB Time-Series Collection Creation**  
  - **Dynamic retention settings for logs** (configurable purge duration).  
- **🚀 Optimized Logging with Serilog**:  
  - Supports **MongoDB logging**.  
  - **No duplicate console logs** (prevents redundant output).  
  - Writes structured logs to MongoDB for easy querying.  
  - Startup & shutdown logs **explicitly handled** for diagnostics.  
- **🔒 Secure HTTPS Support**:  
  - Reads TLS settings from `specmetrix.json`.  
  - Auto-configures **Kestrel** for **secure** API access.  

---

## **📁 Solution Overview**
The SpecMetrix logging solution consists of **three key components**:

### **1️⃣ MongoDB Data Service**
- A **separate generic service** to **read/write logs** to MongoDB.
- **Automated Time-Series Collection Setup** for logs.
- Configurable **log retention period** (defaults to **7 days** via TTL settings).
- Optimized queries for **fast log retrieval**.

### **2️⃣ SpecMetrixLog Service**
- Runs **as a background service** (Windows/Linux compatible).
- **Processes incoming logs** from SpecMetrix.
- Employs **deduplication filter** for **high-speed log ingestion**.
- Uses **caching** to avoid unnecessary database reads.
- **Automatically ensures MongoDB database and collection exist.**

---

## **🔧 Configuration Files**
### **📜 `specmetrix.json` (Primary Config)**
Located at `C:\Configurations\specmetrix.json`.  
Defines:
- **Logging settings** (MongoDB, Console, etc.).
- **MongoDB Time-Series Settings**:
  - `Granularity`: Set as `hours`, `minutes`, or `seconds`.
  - `ExpireAfterDays`: Defines **automatic log retention**.
- **TLS/HTTPS certificates** for secure communication.
- **Service-specific configurations**.

---

## **💡 Notes & Considerations**
- **Logging Redundancy Prevention**:  
  - Console logs are **disabled if MongoDB logging is enabled** to prevent **duplicate logs**.  
- **Windows Service Optimization**:  
  - The application **automatically detects** if running as a **Windows Service** and configures itself accordingly.
- **MongoDB Collection Auto-Creation**:  
  - If the `Logs` collection does not exist, it is **automatically created as a Time-Series Collection**.
  - Ensures correct **granularity & expiration settings**.
- **Future Enhancements Planned**:  
  - **WebSocket API for real-time log streaming.**  
  - **AI-powered anomaly detection for log analysis.**  

---

## **📜 License**
This project is licensed under the **MIT License**.

---

## **📞 Support & Contact**
For assistance, contact **SpecMetrix Support** at:  
📧 **greensboro.support@industrialphysics.com**  
🌐 **https://industrialphysics.com/brands/specmetrix/**  

---