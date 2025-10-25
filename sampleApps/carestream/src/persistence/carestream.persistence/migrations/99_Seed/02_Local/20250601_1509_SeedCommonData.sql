-- 20250601_1509_SeedCommonData.sql
-- Seeds staff reports and internal communication data.

SET search_path TO app, public;

-- Staff Reports (Patient Admin Dashboard - Recent Staff Reports)
INSERT INTO staff_reports (author_user_id, title, priority, content, created_at, facility_id, department_id) VALUES
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), 'Emergency Protocol Update', 'High', 'Updated emergency response protocols for 2 Military Hospital. All staff must review and acknowledge.', NOW() - INTERVAL '1 day 2 hours', (SELECT facility_id FROM facilities WHERE short_code = '2MH'), (SELECT department_id FROM departments WHERE name = 'Emergency' AND facility_id = (SELECT facility_id FROM facilities WHERE short_code = '2MH'))),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'), 'Daily Ward Round Summary', 'Medium', 'Summary of today''s ward rounds at 2MH Internal Medicine, patient status updates, and treatment modifications.', NOW() - INTERVAL '1 day 5 hours', (SELECT facility_id FROM facilities WHERE short_code = '2MH'), (SELECT department_id FROM departments WHERE name = 'Internal Medicine' AND facility_id = (SELECT facility_id FROM facilities WHERE short_code = '2MH'))),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_sysadmin_1'), 'Medical Supply Inventory Alert', 'High', 'Critical medical supplies running low at 2 Military Hospital. Immediate restocking required.', NOW() - INTERVAL '1 day 8 hours', (SELECT facility_id FROM facilities WHERE short_code = '2MH'), (SELECT department_id FROM departments WHERE name = 'Supply Chain' AND facility_id = (SELECT facility_id FROM facilities WHERE short_code = '2MH'))),
((SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_2'), 'New Patient Registration Procedure', 'Normal', 'New guidelines for patient registration at 1 Military Hospital. Review section 3.2.1.', NOW() - INTERVAL '6 hours', (SELECT facility_id FROM facilities WHERE short_code = '1MH'), (SELECT department_id FROM departments WHERE name = 'Emergency' AND facility_id = (SELECT facility_id FROM facilities WHERE short_code = '1MH')));


-- Conversations & Messages (for internal messaging UI)
-- Conversation 1: Sarah Mitchell (PatientAdmin) and Themba Zulu (Doctor)
INSERT INTO conversations (name, is_group_chat, created_at, created_by_user_id, last_message_at) VALUES
('Chat: Sarah & Themba', FALSE, NOW() - INTERVAL '2 hours', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), NOW());

INSERT INTO messages (conversation_id, sender_user_id, content, sent_at, is_read) VALUES
((SELECT conversation_id FROM conversations WHERE name = 'Chat: Sarah & Themba'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), 'Hi Dr. Zulu, patient Michael Chen is ready for you in Room 3.', NOW() - INTERVAL '1 hour 55 minutes', TRUE),
((SELECT conversation_id FROM conversations WHERE name = 'Chat: Sarah & Themba'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'), 'Thanks, I''m on my way.', NOW() - INTERVAL '1 hour 50 minutes', TRUE);

INSERT INTO conversation_participants (conversation_id, user_id, joined_at, last_viewed_at) VALUES
((SELECT conversation_id FROM conversations WHERE name = 'Chat: Sarah & Themba'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), NOW() - INTERVAL '2 hours', NOW()),
((SELECT conversation_id FROM conversations WHERE name = 'Chat: Sarah & Themba'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'), NOW() - INTERVAL '2 hours', NOW());

-- Conversation 2: Emily Johnson (Nurse) and Abe Moshou (Pharmacist)
INSERT INTO conversations (name, is_group_chat, created_at, created_by_user_id, last_message_at) VALUES
('Chat: Emily & Abe', FALSE, NOW() - INTERVAL '1 hour', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_nurse_1'), NOW());

INSERT INTO messages (conversation_id, sender_user_id, content, sent_at, is_read) VALUES
((SELECT conversation_id FROM conversations WHERE name = 'Chat: Emily & Abe'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_nurse_1'), 'Checking if Lisinopril stock is available for PTE10000001?', NOW() - INTERVAL '50 minutes', FALSE),
((SELECT conversation_id FROM conversations WHERE name = 'Chat: Emily & Abe'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_pharmacist_1'), 'Yes, we have sufficient stock. Prescriptions for him are pending.', NOW() - INTERVAL '45 minutes', TRUE);

INSERT INTO conversation_participants (conversation_id, user_id, joined_at, last_viewed_at) VALUES
((SELECT conversation_id FROM conversations WHERE name = 'Chat: Emily & Abe'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_nurse_1'), NOW() - INTERVAL '1 hour', NOW() - INTERVAL '40 minutes'), -- Nurse has unread message
((SELECT conversation_id FROM conversations WHERE name = 'Chat: Emily & Abe'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_pharmacist_1'), NOW() - INTERVAL '1 hour', NOW());