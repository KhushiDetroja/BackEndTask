USE TicketManagementSystem
GO

CREATE TABLE Users 
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL, 
    RoleId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

INSERT INTO Users (Name, Email, Password, RoleId)
VALUES (
    'Admin User',
    'admin@test.com',
    '$2b$12$HzQ5Av16arIYHOMQxVV7Me3/hfXDG49rC7aGp6TO2aBFLv4qDRFbC', 
    1
);

SELECT * FROM Users

SELECT * FROM Tickets

--admin@test.com - Admin@123
--support@test.com - Support@123
--user@test.com - User@123

