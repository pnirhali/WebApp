# WebApp
- This is part of my learing about  OAuth 2 using .Net core Web API 3.0 without using 3rd party libraries
- It has 3 API : Register, Login, ConfirmEmail.
- Entity framework core has been used to interact with SQL server database.

# Register:
- Used  MD5 - salted hashing alogrithm(one way encryption) to encrypt password.
- "SendGrid" email provider has been used to send an email confirmation once user account gets created.
- Used Two way encryption for email confirmation token with AES algorithm.

# ConfirmEmail:
- Decrypted and validated email confirmation token.
- Updated user's "EmailConfirmed" flag from DB.

# Login:
- Validated user's email address and password.
- One way encryption has been used to generate hashing password.
- Generated an Access token with expiry date time and userId.
- This action method returns AccessToken if user enters the correct email and password.

