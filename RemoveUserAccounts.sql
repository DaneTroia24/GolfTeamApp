-- Just clear Identity tables (keep your golf data)
DELETE FROM AspNetUserRoles;
DELETE FROM AspNetUserClaims;
DELETE FROM AspNetUserLogins;
DELETE FROM AspNetUserTokens;
DELETE FROM AspNetUsers;

-- Clear the UserId links in your models
UPDATE Coaches SET UserId = NULL;
UPDATE Partners SET UserId = NULL;
UPDATE Athletes SET UserId = NULL;