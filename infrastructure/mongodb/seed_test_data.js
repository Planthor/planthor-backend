// Planthor MongoDB Test Data Seed Script
// Usage: mongosh "<connection-string>" --file infrastructure/mongodb/seed_test_data.js
//
// The IdentifyName values below must exactly match the Keycloak usernames for
// MemberSessionFilter auto-provisioning to recognise existing members.

const DB_NAME = "planthor-dev";

const db_conn = db.getSiblingDB(DB_NAME);

// ─── Helpers ─────────────────────────────────────────────────────────────────

function newUUID() {
  // Generate a v4-like UUID using Math.random (sufficient for seed data)
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

function toInstant(dateStr) {
  // Store as ISO 8601 string matching NodaTime Instant serialisation
  return new Date(dateStr).toISOString();
}

const NOW = toInstant(new Date().toISOString());
const SYSTEM_ID = "00000000-0000-0000-0000-000000000000";

// ─── Members ─────────────────────────────────────────────────────────────────
// IdentifyName must match the Keycloak username (sub claim / preferred_username).
// Adjust if your Keycloak token uses a different claim for IdentifyName.

const member1Id = newUUID();
const member2Id = newUUID();
const member3Id = newUUID();

const members = [
  {
    _id: member1Id,
    IdentifyName: "testuser1",
    FirstName: "Alice",
    MiddleName: "",
    LastName: "Runner",
    Description: "Marathon enthusiast and trail runner.",
    PathAvatar: null,
    PreferredTimezone: "Asia/Ho_Chi_Minh",
    ExternalConnections: [],
    PersonalPlans: [],
    DomainEvents: [],
    CreatedAt: NOW,
    CreatedBy: SYSTEM_ID,
    LastUpdatedAt: NOW,
    LastUpdatedBy: SYSTEM_ID,
  },
  {
    _id: member2Id,
    IdentifyName: "testuser2",
    FirstName: "Bob",
    MiddleName: "",
    LastName: "Cyclist",
    Description: "Weekend cyclist, loves long-distance rides.",
    PathAvatar: null,
    PreferredTimezone: "Europe/London",
    ExternalConnections: [],
    PersonalPlans: [],
    DomainEvents: [],
    CreatedAt: NOW,
    CreatedBy: SYSTEM_ID,
    LastUpdatedAt: NOW,
    LastUpdatedBy: SYSTEM_ID,
  },
  {
    _id: member3Id,
    IdentifyName: "testuser3",
    FirstName: "Carol",
    MiddleName: "Anne",
    LastName: "Hiker",
    Description: "Passionate hiker and outdoor adventurer.",
    PathAvatar: null,
    PreferredTimezone: "America/New_York",
    ExternalConnections: [],
    PersonalPlans: [],
    DomainEvents: [],
    CreatedAt: NOW,
    CreatedBy: SYSTEM_ID,
    LastUpdatedAt: NOW,
    LastUpdatedBy: SYSTEM_ID,
  },
];

// ─── Plans ────────────────────────────────────────────────────────────────────
// Each plan mirrors the Plan aggregate root in src/Domain/Plans/Plan.cs.
// Status values match PlanStatus enum: 0 = Active, 1 = Completed, 2 = Expired.

const plan1Id = newUUID(); // Alice – Running plan
const plan2Id = newUUID(); // Alice – Hiking plan
const plan3Id = newUUID(); // Bob   – Cycling plan
const plan4Id = newUUID(); // Bob   – Running plan
const plan5Id = newUUID(); // Carol – Hiking plan
const plan6Id = newUUID(); // Carol – Cycling plan

const plans = [
  {
    _id: plan1Id,
    MemberId: member1Id,
    Name: "May Running Challenge",
    Unit: "km",
    Target: 100.0,
    CurrentValue: 34.5,
    From: toInstant("2026-05-01T00:00:00Z"),
    To: toInstant("2026-05-31T23:59:59Z"),
    StartDateLocal: "2026-05-01",
    EndDateLocal: "2026-05-31",
    Timezone: "Asia/Ho_Chi_Minh",
    EnableActivityLog: true,
    Completed: false,
    Status: 0,
    LikeCount: 3,
    SportPlanDetails: {
      SportTypes: ["Run", "TrailRun"],
    },
    ActivityLogs: [],
    DomainEvents: [],
    CreatedAt: NOW,
    CreatedBy: member1Id,
    LastUpdatedAt: NOW,
    LastUpdatedBy: member1Id,
  },
  {
    _id: plan2Id,
    MemberId: member1Id,
    Name: "Spring Hiking Goal",
    Unit: "km",
    Target: 50.0,
    CurrentValue: 12.0,
    From: toInstant("2026-04-01T00:00:00Z"),
    To: toInstant("2026-06-30T23:59:59Z"),
    StartDateLocal: "2026-04-01",
    EndDateLocal: "2026-06-30",
    Timezone: "Asia/Ho_Chi_Minh",
    EnableActivityLog: true,
    Completed: false,
    Status: 0,
    LikeCount: 1,
    SportPlanDetails: {
      SportTypes: ["Hike", "Walk"],
    },
    ActivityLogs: [],
    DomainEvents: [],
    CreatedAt: NOW,
    CreatedBy: member1Id,
    LastUpdatedAt: NOW,
    LastUpdatedBy: member1Id,
  },
  {
    _id: plan3Id,
    MemberId: member2Id,
    Name: "Century Ride Q2",
    Unit: "km",
    Target: 500.0,
    CurrentValue: 210.0,
    From: toInstant("2026-04-01T00:00:00Z"),
    To: toInstant("2026-06-30T23:59:59Z"),
    StartDateLocal: "2026-04-01",
    EndDateLocal: "2026-06-30",
    Timezone: "Europe/London",
    EnableActivityLog: true,
    Completed: false,
    Status: 0,
    LikeCount: 7,
    SportPlanDetails: {
      SportTypes: ["Ride", "VirtualRide"],
    },
    ActivityLogs: [],
    DomainEvents: [],
    CreatedAt: NOW,
    CreatedBy: member2Id,
    LastUpdatedAt: NOW,
    LastUpdatedBy: member2Id,
  },
  {
    _id: plan4Id,
    MemberId: member2Id,
    Name: "5K Running Streak",
    Unit: "km",
    Target: 30.0,
    CurrentValue: 30.0,
    From: toInstant("2026-03-01T00:00:00Z"),
    To: toInstant("2026-03-31T23:59:59Z"),
    StartDateLocal: "2026-03-01",
    EndDateLocal: "2026-03-31",
    Timezone: "Europe/London",
    EnableActivityLog: true,
    Completed: true,
    Status: 1,
    LikeCount: 5,
    SportPlanDetails: {
      SportTypes: ["Run"],
    },
    ActivityLogs: [],
    DomainEvents: [],
    CreatedAt: toInstant("2026-03-01T00:00:00Z"),
    CreatedBy: member2Id,
    LastUpdatedAt: NOW,
    LastUpdatedBy: member2Id,
  },
  {
    _id: plan5Id,
    MemberId: member3Id,
    Name: "Appalachian Section Hike",
    Unit: "km",
    Target: 80.0,
    CurrentValue: 45.0,
    From: toInstant("2026-05-01T00:00:00Z"),
    To: toInstant("2026-07-31T23:59:59Z"),
    StartDateLocal: "2026-05-01",
    EndDateLocal: "2026-07-31",
    Timezone: "America/New_York",
    EnableActivityLog: true,
    Completed: false,
    Status: 0,
    LikeCount: 2,
    SportPlanDetails: {
      SportTypes: ["Hike"],
    },
    ActivityLogs: [],
    DomainEvents: [],
    CreatedAt: NOW,
    CreatedBy: member3Id,
    LastUpdatedAt: NOW,
    LastUpdatedBy: member3Id,
  },
  {
    _id: plan6Id,
    MemberId: member3Id,
    Name: "Gravel Cycling Adventure",
    Unit: "km",
    Target: 200.0,
    CurrentValue: 68.0,
    From: toInstant("2026-05-01T00:00:00Z"),
    To: toInstant("2026-05-31T23:59:59Z"),
    StartDateLocal: "2026-05-01",
    EndDateLocal: "2026-05-31",
    Timezone: "America/New_York",
    EnableActivityLog: true,
    Completed: false,
    Status: 0,
    LikeCount: 0,
    SportPlanDetails: {
      SportTypes: ["GravelRide", "Ride"],
    },
    ActivityLogs: [],
    DomainEvents: [],
    CreatedAt: NOW,
    CreatedBy: member3Id,
    LastUpdatedAt: NOW,
    LastUpdatedBy: member3Id,
  },
];

// ─── PersonalPlan links (embedded in Member) ─────────────────────────────────

members[0].PersonalPlans = [
  {
    _id: newUUID(),
    MemberId: member1Id,
    PlanId: plan1Id,
    DisplayOnProfile: true,
    Prioritize: 0,
    LinkUserAdapter: true,
    CreatedAt: NOW,
    CreatedBy: member1Id,
    LastUpdatedAt: NOW,
    LastUpdatedBy: member1Id,
  },
  {
    _id: newUUID(),
    MemberId: member1Id,
    PlanId: plan2Id,
    DisplayOnProfile: true,
    Prioritize: 1,
    LinkUserAdapter: false,
    CreatedAt: NOW,
    CreatedBy: member1Id,
    LastUpdatedAt: NOW,
    LastUpdatedBy: member1Id,
  },
];

members[1].PersonalPlans = [
  {
    _id: newUUID(),
    MemberId: member2Id,
    PlanId: plan3Id,
    DisplayOnProfile: true,
    Prioritize: 0,
    LinkUserAdapter: true,
    CreatedAt: NOW,
    CreatedBy: member2Id,
    LastUpdatedAt: NOW,
    LastUpdatedBy: member2Id,
  },
  {
    _id: newUUID(),
    MemberId: member2Id,
    PlanId: plan4Id,
    DisplayOnProfile: true,
    Prioritize: 1,
    LinkUserAdapter: false,
    CreatedAt: NOW,
    CreatedBy: member2Id,
    LastUpdatedAt: NOW,
    LastUpdatedBy: member2Id,
  },
];

members[2].PersonalPlans = [
  {
    _id: newUUID(),
    MemberId: member3Id,
    PlanId: plan5Id,
    DisplayOnProfile: true,
    Prioritize: 0,
    LinkUserAdapter: true,
    CreatedAt: NOW,
    CreatedBy: member3Id,
    LastUpdatedAt: NOW,
    LastUpdatedBy: member3Id,
  },
  {
    _id: newUUID(),
    MemberId: member3Id,
    PlanId: plan6Id,
    DisplayOnProfile: false,
    Prioritize: 1,
    LinkUserAdapter: false,
    CreatedAt: NOW,
    CreatedBy: member3Id,
    LastUpdatedAt: NOW,
    LastUpdatedBy: member3Id,
  },
];

// ─── Insert ───────────────────────────────────────────────────────────────────

print("Seeding Members collection...");
const memberResult = db_conn.Members.insertMany(members);
print(`Inserted ${Object.keys(memberResult.insertedIds).length} members.`);

print("Seeding Plans collection...");
const planResult = db_conn.Plans.insertMany(plans);
print(`Inserted ${Object.keys(planResult.insertedIds).length} plans.`);

print("");
print("Seed complete. Verify with:");
print(`  use ${DB_NAME}`);
print("  db.Members.find().pretty()");
print("  db.Plans.find().pretty()");
