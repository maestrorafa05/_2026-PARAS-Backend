# PARAS Backend API

**PARAS** (Platform Peminjaman Ruangan dan Sarana) adalah sistem backend untuk mengelola peminjaman ruangan dengan fitur lengkap meliputi manajemen ruangan, peminjaman, validasi ketersediaan, dan persetujuan status peminjaman.

---

## Daftar Isi

1. [Teknologi & Arsitektur](#-teknologi--arsitektur)
2. [Cara Menjalankan Aplikasi](#-cara-menjalankan-aplikasi)
3. [Konfigurasi User Secrets](#-konfigurasi-user-secrets)
4. [Migration Commands](#-migration-commands)
5. [Daftar Endpoint & Contoh Request](#-daftar-endpoint--contoh-request)
6. [Business Rules](#-business-rules)

---

## Teknologi & Arsitektur

### Tech Stack
- **.NET 10.0** - Framework utama
- **ASP.NET I** - Web API framework
- **Entity Framework Core 10.0** - ORM untuk database access
- **SQL Server** - Database
- **Scalar** - API Documentation (OpenAPI/Swagger alternative)

### Struktur Folder
```
PARAS.Api/
├── Data/               # DbContext & database seeding
├── Domain/
│   ├── Entities/       # Entity models (Loan, Room, etc)
│   └── Enums/          # LoanStatus enum
├── DTOs/               # Request & Response DTOs
├── Endpoints/          # API endpoints (Minimal API)
├── Services/           # Business logic & validators
├── Options/            # Configuration options
├── Migrations/         # EF Core migrations
└── Program.cs          # Application entry point
```

### Fitur Utama
- ✅ **CRUD Ruangan** - Kelola data ruangan (active/inactive status)
- ✅ **Manajemen Peminjaman** - Create, read, cancel loan requests
- ✅ **Status Workflow** - Pending → Approved/Rejected/Cancelled dengan history tracking
- ✅ **Validasi Ketersediaan** - Cek bentrok jadwal & availability
- ✅ **Business Rules Validation** - Jam operasional, durasi, advance booking
- ✅ **Health Checks** - Monitor database connectivity
- ✅ **CORS Support** - Frontend integration ready
- ✅ **Problem Details** - Standardized error responses

---

## Cara Menjalankan Aplikasi

### Prerequisites
- .NET 10 SDK ([Download](https://dotnet.microsoft.com/download))
- SQL Server (LocalDB, Express, atau Developer Edition)
- Visual Studio 2025 / VS Code / Rider (opsional)

### Langkah-langkah

1. **Clone repository**
   ```bash
   git clone <repository-url>
   cd _2026-PARAS-Backend/PARAS.Api
   ```

2. **Konfigurasi Connection String**
   
   Buat atau edit `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=paras_app;Integrated Security=True;TrustServerCertificate=True;"
     }
   }
   ```

   Atau gunakan **User Secrets** (recommended untuk production):
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=paras_app;Integrated Security=True;TrustServerCertificate=True;"
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Jalankan database migrations**
   ```bash
   dotnet ef database update
   ```

5. **Run aplikasi**
   ```bash
   dotnet run
   ```

6. **Akses API Documentation**
   - Buka browser: `https://localhost:7xxx/scalar/v1` (port akan ditampilkan di console)
   - Atau gunakan endpoint root: `https://localhost:7xxx/`

---

## Konfigurasi User Secrets

User Secrets digunakan untuk menyimpan data sensitif (seperti connection string) tanpa commit ke repository.

### User Secrets ID
```xml
<UserSecretsId>da6f9d28-07ef-4494-8e56-5c951526ec8e</UserSecretsId>
```

### Set User Secrets

```bash
# Set connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=paras_app;Integrated Security=True;TrustServerCertificate=True;"

# Set booking rules (opsional, override appsettings)
dotnet user-secrets set "BookingRules:OpenTime" "08:00"
dotnet user-secrets set "BookingRules:CloseTime" "21:00"
dotnet user-secrets set "BookingRules:MaxAdvanceDays" "60"
```

### List User Secrets
```bash
dotnet user-secrets list
```

### Remove User Secrets
```bash
dotnet user-secrets remove "ConnectionStrings:DefaultConnection"
```

### Clear All Secrets
```bash
dotnet user-secrets clear
```

---

## Migration Commands

### Membuat Migration Baru
```bash
dotnet ef migrations add NamaMigration
```

Contoh:
```bash
dotnet ef migrations add AddUserTable
dotnet ef migrations add UpdateLoanStatusEnum
```

### Apply Migration ke Database
```bash
# Update ke migration terbaru
dotnet ef database update

# Update ke migration tertentu
dotnet ef database update NamaMigration

# Rollback ke migration sebelumnya
dotnet ef database update PreviousMigrationName
```

### Remove Migration Terakhir
```bash
# Hapus migration yang belum di-apply
dotnet ef migrations remove
```

### List Migrations
```bash
dotnet ef migrations list
```

### Generate SQL Script
```bash
# Generate SQL dari semua migrations
dotnet ef migrations script

# Generate SQL untuk migration tertentu
dotnet ef migrations script FromMigration ToMigration

# Save to file
dotnet ef migrations script -o migration.sql
```

### Database Drop (Hati-hati!)
```bash
dotnet ef database drop
```

---

## Daftar Endpoint & Contoh Request

Base URL: `https://localhost:7xxx` (cek console saat run untuk port pasti)

---

### System Endpoints

#### 1. Check Service Status
```http
GET /
```

**Response:**
```json
{
  "service": "PARAS.Api",
  "status": "up"
}
```

#### 2. Health Check
```http
GET /health
```

#### 3. Database Connectivity Check
```http
GET /db-ping
```

**Response:**
```json
{
  "canConnect": true
}
```

---

### Room Endpoints

#### 1. Get All Rooms
```http
GET /rooms
```

**Response:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "code": "A101",
    "name": "Ruang Meeting A",
    "location": "Lantai 1",
    "capacity": 20,
    "facilities": "Proyektor, AC, Whiteboard",
    "isActive": true,
    "createdAt": "2026-02-16T10:00:00Z",
    "updatedAt": "2026-02-16T10:00:00Z"
  }
]
```

#### 2. Get Room by ID
```http
GET /rooms/{id}
```

**Example:**
```http
GET /rooms/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

#### 3. Create Room
```http
POST /rooms
Content-Type: application/json

{
  "code": "A101",
  "name": "Ruang Meeting A",
  "location": "Lantai 1",
  "capacity": 20,
  "facilities": "Proyektor, AC, Whiteboard"
}
```

**Response (201 Created):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "A101",
  "name": "Ruang Meeting A",
  "location": "Lantai 1",
  "capacity": 20,
  "facilities": "Proyektor, AC, Whiteboard",
  "isActive": true,
  "createdAt": "2026-02-16T10:00:00Z",
  "updatedAt": "2026-02-16T10:00:00Z"
}
```

#### 4. Update Room
```http
PUT /rooms/{id}
Content-Type: application/json

{
  "code": "A101",
  "name": "Ruang Meeting A (Updated)",
  "location": "Lantai 1",
  "capacity": 25,
  "facilities": "Proyektor, AC, Whiteboard, Sound System",
  "isActive": true
}
```

#### 5. Check Room Availability
```http
GET /rooms/available?start=2026-02-17T09:00:00&end=2026-02-17T11:00:00
```

**Response:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "code": "A101",
    "name": "Ruang Meeting A",
    "location": "Lantai 1",
    "capacity": 20,
    "facilities": "Proyektor, AC, Whiteboard"
  }
]
```

#### 6. Check Specific Room Availability
```http
GET /rooms/{id}/availability?start=2026-02-17T09:00:00&end=2026-02-17T11:00:00
```

**Response:**
```json
{
  "roomId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "start": "2026-02-17T09:00:00",
  "end": "2026-02-17T11:00:00",
  "isAvailable": false,
  "conflicts": [
    {
      "id": "abc123...",
      "startTime": "2026-02-17T08:30:00",
      "endTime": "2026-02-17T10:30:00",
      "status": "approved"
    }
  ]
}
```

---

### Loan Endpoints

#### 1. Get All Loans
```http
GET /loans
```

**Response:**
```json
[
  {
    "id": "abc123-...",
    "roomId": "3fa85f64-...",
    "roomCode": "A101",
    "roomName": "Ruang Meeting A",
    "namaPeminjam": "John Doe",
    "nrp": "2024001",
    "startTime": "2026-02-17T09:00:00",
    "endTime": "2026-02-17T11:00:00",
    "status": "pending",
    "notes": "Meeting tim project",
    "createdAt": "2026-02-16T10:00:00Z",
    "updatedAt": "2026-02-16T10:00:00Z"
  }
]
```

#### 2. Get Loan by ID
```http
GET /loans/{id}
```

#### 3. Create Loan
```http
POST /loans
Content-Type: application/json

{
  "roomId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "namaPeminjam": "John Doe",
  "nrp": "2024001",
  "startTime": "2026-02-17T09:00:00",
  "endTime": "2026-02-17T11:00:00",
  "notes": "Meeting tim project"
}
```

**Validation Rules (akan divalidasi):**
- StartTime < EndTime
- Durasi: 30-240 menit (default)
- Waktu: 07:00-20:00 (default)
- Minimal booking: H+10 menit
- Maksimal booking: H+30 hari
- Room harus aktif
- Tidak boleh bentrok dengan loan lain (yang tidak rejected/cancelled)

**Response (201 Created):**
```json
{
  "id": "abc123-...",
  "roomId": "3fa85f64-...",
  "roomCode": "A101",
  "roomName": "Ruang Meeting A",
  "namaPeminjam": "John Doe",
  "nrp": "2024001",
  "startTime": "2026-02-17T09:00:00",
  "endTime": "2026-02-17T11:00:00",
  "status": "pending",
  "notes": "Meeting tim project",
  "createdAt": "2026-02-16T10:00:00Z",
  "updatedAt": "2026-02-16T10:00:00Z"
}
```

**Error Example (400 Bad Request):**
```json
{
  "errors": [
    "Durasi minimal 30 menit.",
    "Booking hanya boleh antara 07:00 - 20:00."
  ]
}
```

#### 4. Cancel Loan (Delete)
```http
DELETE /loans/{id}
```

**Response (204 No Content)** - Loan status diubah menjadi `cancelled`

**Error:**
- 404 Not Found - Loan tidak ditemukan
- 409 Conflict - Loan sudah rejected/cancelled

#### 5. Change Loan Status
```http
PATCH /loans/{id}/status
Content-Type: application/json

{
  "toStatus": "approved",
  "admin": "admin@example.com",
  "comment": "Disetujui untuk keperluan rapat tim"
}
```

**Valid Status Transitions:**
- `pending` → `approved` | `rejected` | `cancelled`
- `approved` → `cancelled`
- `rejected` → (tidak bisa diubah)
- `cancelled` → (tidak bisa diubah)

**Validation untuk Approve:**
- Cek tidak ada loan `approved` lain yang bentrok jadwal

**Response (200 OK):**
```json
{
  "loanId": "abc123-...",
  "fromStatus": "pending",
  "toStatus": "approved",
  "changedAt": "2026-02-16T10:30:00Z"
}
```

**Error Example (409 Conflict):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Tidak bisa approve: jadwal bentrok dengan loan Approved lain."
}
```

#### 6. Get Loan Status History
```http
GET /loans/{id}/history
```

**Response:**
```json
[
  {
    "id": "hist123-...",
    "loanId": "abc123-...",
    "fromStatus": "pending",
    "toStatus": "approved",
    "admin": "admin@example.com",
    "comment": "Disetujui untuk keperluan rapat tim",
    "changedAt": "2026-02-16T10:30:00Z"
  }
]
```

---

## Business Rules

### Booking Rules (Konfigurasi di appsettings.json)

```json
{
  "BookingRules": {
    "OpenTime": "07:00",           // Jam buka
    "CloseTime": "20:00",          // Jam tutup
    "MinDurationMinutes": 30,      // Durasi minimal (menit)
    "MaxDurationMinutes": 240,     // Durasi maksimal (menit)
    "MaxAdvanceDays": 30,          // Booking paling lama H+N hari
    "MinLeadMinutes": 10,          // Booking paling cepat H+N menit
    "BufferMinutesBetweenBookings": 0,  // Buffer antar booking (future use)
    "AllowWeekend": true           // Izinkan booking weekend
  }
}
```

### Loan Status Workflow

```
pending ──┬──> approved ──> cancelled
          ├──> rejected (final)
          └──> cancelled (final)
```

**Rules:**
- `pending` dapat diubah ke `approved`, `rejected`, atau `cancelled`
- `approved` hanya dapat di-`cancel`
- `rejected` dan `cancelled` bersifat **final** (tidak bisa diubah)

### Conflict Detection
- Sistem otomatis cek bentrok jadwal saat:
  - Create loan baru
  - Approve loan
- Yang dianggap bentrok: loan dengan status **bukan** `rejected` atau `cancelled`
- Algoritma: `(new.start < existing.end) && (new.end > existing.start)`

---

## Referensi Entity

### LoanStatus Enum
```csharp
public enum LoanStatus
{
    pending = 0,    // Menunggu approval
    approved = 1,   // Disetujui
    rejected = 2,   // Ditolak (final)
    cancelled = 3,  // Dibatalkan (final)
    completed = 4   // Selesai (future use)
}
```

### Core Entities
- **Room** - Data ruangan
- **Loan** - Data peminjaman
- **LoanStatusHistory** - Riwayat perubahan status

---

## Testing

### Via Scalar UI
1. Run aplikasi: `dotnet run`
2. Buka `https://localhost:7xxx/scalar/v1`
3. Explore & test endpoints langsung dari browser

### Via HTTP File (VS Code/Rider)
Gunakan file `PARAS.Api.http` untuk quick testing.

---

## License

## Contributors

Maestro Rafa Agniya
312460007
