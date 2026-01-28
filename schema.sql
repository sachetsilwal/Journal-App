-- Project Journal App - SQLite Schema
-- This script creates all tables required for the journaling application.

-- 1. Users table
create table if not exists "Users" (
   "UserId"       integer primary key,
   "Username"     text not null unique,
   "PasswordHash" text not null,
   "CreatedAt"    datetime not null,
   "LastLogin"    datetime
);

-- 2. Categories table
create table if not exists "Categories" (
   "CategoryId"  integer primary key,
   "Name"        text not null,
   "Description" text
);

-- 3. JournalEntries table
create table if not exists "JournalEntries" (
   "EntryId"    integer primary key,
   "UserId"     integer not null,
   "Title"      text,
   "Content"    text not null,
   "EntryDate"  text not null,
   "CategoryId" integer,
   "WordCount"  integer not null,
   "CreatedAt"  datetime not null,
   "UpdatedAt"  datetime not null,
   foreign key ( "UserId" )
      references "Users" ( "UserId" )
         on delete cascade,
   foreign key ( "CategoryId" )
      references "Categories" ( "CategoryId" )
         on delete set null
);

-- 4. Tags table
create table if not exists "Tags" (
   "TagId"     integer primary key,
   "UserId"    integer not null,
   "Name"      text not null,
   "Color"     text,
   "CreatedAt" datetime not null,
   foreign key ( "UserId" )
      references "Users" ( "UserId" )
         on delete cascade
);

-- 5. EntryTags table (Many-to-Many association)
create table if not exists "EntryTags" (
   "Id"        integer primary key,
   "EntryId"   integer not null,
   "TagId"     integer not null,
   "CreatedAt" datetime not null,
   foreign key ( "EntryId" )
      references "JournalEntries" ( "EntryId" )
         on delete cascade,
   foreign key ( "TagId" )
      references "Tags" ( "TagId" )
         on delete cascade
);

-- 6. Moods table (Reference data)
create table if not exists "Moods" (
   "MoodId"    integer primary key,
   "Name"      text not null,
   "Icon"      text not null,
   "Color"     text,
   "Intensity" integer not null,
   "Category"  text not null
);

-- 7. EntryMoods table (Many-to-Many association)
create table if not exists "EntryMoods" (
   "Id"        integer primary key,
   "EntryId"   integer not null,
   "MoodId"    integer not null,
   "Intensity" integer,
   "IsPrimary" integer not null, -- Boolean handled as 0/1 in SQLite
   "CreatedAt" datetime not null,
   foreign key ( "EntryId" )
      references "JournalEntries" ( "EntryId" )
         on delete cascade,
   foreign key ( "MoodId" )
      references "Moods" ( "MoodId" )
         on delete cascade
);

-- 8. Streaks table
create table if not exists "Streaks" (
   "StreakId"  integer primary key,
   "UserId"    integer not null,
   "StartDate" text not null,
   "EndDate"   text,
   "DayCount"  integer not null,
   "IsActive"  integer not null, -- Boolean handled as 0/1 in SQLite
   "CreatedAt" datetime not null,
   "UpdatedAt" datetime not null,
   foreign key ( "UserId" )
      references "Users" ( "UserId" )
         on delete cascade
);

-- 9. UserSettings table
create table if not exists "UserSettings" (
   "SettingId"    integer primary key,
   "UserId"       integer not null,
   "SettingKey"   text not null,
   "SettingValue" text not null,
   "CreatedAt"    datetime not null,
   "UpdatedAt"    datetime not null,
   foreign key ( "UserId" )
      references "Users" ( "UserId" )
         on delete cascade
);

-- Create indexes for performance
create index if not exists "IX_JournalEntries_UserId_EntryDate" on
   "JournalEntries" (
      "UserId",
      "EntryDate"
   );
create index if not exists "IX_EntryTags_EntryId" on
   "EntryTags" (
      "EntryId"
   );
create index if not exists "IX_EntryMoods_EntryId" on
   "EntryMoods" (
      "EntryId"
   );
create index if not exists "IX_UserSettings_UserId_SettingKey" on
   "UserSettings" (
      "UserId",
      "SettingKey"
   );