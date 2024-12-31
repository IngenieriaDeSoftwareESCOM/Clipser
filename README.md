# Running the Multi-Service Docker Application

This guide explains how to set up and run the services defined in the `docker-compose.yml` file on **Windows**, **Linux**, and **MacOS**.

## Prerequisites
1. **Docker** and **Docker Compose** installed on your system:
   - [Docker Desktop (Windows & MacOS)](https://www.docker.com/products/docker-desktop/)
   - [Docker Engine (Linux)](https://docs.docker.com/engine/install/)

2. Verify the installations by running:
   ```bash
   docker --version
   docker-compose --version
   ```

---

## Steps to Run the Application

### 1. Clone the Repository
If the `docker-compose.yml` and `Dockerfile` are part of a repository, clone it to your local system:
```bash
git clone https://github.com/IngenieriaDeSoftwareESCOM/Clipser.git
cd Clipser
```

---

### 2. Build and Start the Containers
Run the following command to build and start the containers:
```bash
docker-compose up --build
```

### 3. Verify the Services
- **MariaDB**:
  - Accessible on `localhost:3307`.
  - The database name is `Clipser` with credentials:
    - Username: `clipser`
    - Password: `clipser_secure_password_1234.`

- **SurrealDB**:
  - Accessible on `localhost:7979`.
  - Credentials:
    - Username: `root`
    - Password: `root`.

- **dotnet-app**:
  - Accessible on `http://localhost:5000`.

---

## OS-Specific Notes

### **Windows**
- Use **PowerShell** or **Command Prompt** to run the commands.
- Ensure Docker Desktop is running.

### **Linux**
- Open a terminal and ensure you have sufficient permissions to run Docker commands (or prepend with `sudo`).

### **MacOS**
- Use the Terminal app.
- Ensure Docker Desktop is running.

---

## Managing the Application

### Start the Application
```bash
docker-compose up
```

### Stop the Application
```bash
docker-compose down
```

### Rebuild the Containers
If changes are made to the `Dockerfile` or `docker-compose.yml`:
```bash
docker-compose down --volumes
docker-compose up --build
```

---

## Troubleshooting

### Common Issues:
1. **Database Connection Errors**:
   - Ensure the MariaDB service is healthy by checking its logs:
     ```bash
     docker logs mariadb
     ```

2. **SurrealDB Not Starting**:
   - Check its logs:
     ```bash
     docker logs surrealdb
     ```

3. **dotnet-app Fails to Start**:
   - Verify its logs:
     ```bash
     docker logs dotnet-app
     ```

---

## Healthcheck Overview
- MariaDB includes a healthcheck to ensure it is ready before other services depend on it.
- SurrealDB starts immediately without a healthcheck.

---

## Cleanup
To remove all containers, images, and volumes:
```bash
docker-compose down --rmi all --volumes --remove-orphans
```