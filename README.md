# Nexus

## Description
Nexus is a comprehensive platform developed as part of my final year project in Computer Science. It serves as a supportive community for individuals dealing with mental disorders such as depression and stress. The platform allows users to share their thoughts publicly or anonymously and receive advice from other users or professional doctors. Nexus is designed with robust backend solutions using ASP.NET Core, adhering to Onion Architecture principles, clean code practices, and respecting the SOLID principles as much as possible.

## Features

### Core Functionalities
- **Community Support**: Users can share their thoughts and receive comments and advice from both other users and professional doctors.
- **Anonymity**: Option to share thoughts anonymously for privacy.

### Machine Learning Integration
- **Hate Speech and Offensive Language Detection**: Ensures a safe, respectful, and clean community.
- **Depression Detection**: Helps in identifying signs of depression.

### Appointment System
- **Doctor Scheduling**: Doctors can create their weekly schedules.
- **Appointment Booking**: Users can book offline appointments with specific doctors in available slots.
- **Confirmation/Rejection**: Doctors can confirm or reject appointments.

### Notifications and Communication
- **Mailing Service**: Integrated Google SMTP server for enhanced communication.
- **Real-Time Notifications**: Implemented using SignalR.

### Additional Features
- **Image Storage**: Integrated Cloudinary API for storing images.
- **News Integration**: Uses News API to fetch relevant articles for users and doctors.
- **Unified API Response**: Developed using the Result Pattern.
- **Token-Based Authentication**: Implemented JWT Bearer authentication with role-based access mechanisms.
- **External Login Provider**: Integrated Google for external login.
- **Insights**: Provides doctors and admins with insights about appointments, profits, depression test results, etc.

## Development and Deployment
- **Entity Framework Core**: For optimized SQL queries and LINQ operations.
- **Pagination and Filtering**: Implemented for efficient data retrieval.
- **AutoMapper**: For efficient mapping between DTOs and entities.
- **Database Seeding**: Used Bogus library for database seeding to facilitate testing.
- **Containerization**: Application containerized using Docker for seamless deployment.
- **CI/CD Pipeline**: Created using GitHub Actions to automate building, image building, image publishing, and deployment to the production server.
- **Cloud Deployment**: API running on the cloud using Render.

## Technologies Used
- **Backend**: ASP.NET Core 8, Entity Framework Core, SQL Server
- **Containerization**: Docker
- **CI/CD**: GitHub Actions
- **Testing and Documentation**: Postman, Bogus
- **Notifications**: SignalR
- **Mailing**: MailKit/MimeKit, Google SMTP server
- **Image Storage**: Cloudinary API
- **News Integration**: News API

For any inquiries or feedback, please contact me at [alaaebrahim387@gmail.com].
