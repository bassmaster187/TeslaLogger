# Installation of PHPMYADMIN inside TeslaLogger

#### 1. connect via "ssh"
  * sudo apt-get install phpmyadmin
  * confirm all questions with the default

#### 2. connect to [raspberry/phpmyadmin](http://raspberry/phpmyadmin) using the standard credentials (root / teslalogger)

#### 3. if you get "access denied" errors, perform the resolution steps described here [https://www.cyberciti.biz/faq/mysql-change-user-password/]
  * connect again via "ssh"
  * mysql -u root -h localhost -p
   password is "teslalogger"
  * ALTER USER ‚root‘@‚localhost‘ IDENTIFIED BY ‚teslalogger‘;
  * FLUSH PRIVILEGES;
   create a new user to run with phpmyadmin:
  * CREATE USER ‚phpmyadmin‘@localhost IDENTIFIED BY ‚password1‘;
  * GRANT ALL PRIVILEGES ON . TO ‚phpmyadmin‘@localhost IDENTIFIED BY ‚password1‘;
  * exit
   Restart Apache Web Server
  * sudo systemctl restart apache2

#### 4. connect again to phpmyadmin using the credentials just created

#### 5. if you get the error message "count(): Parameter must be an array or an object that implements Countable" while browsing into the data of a table, follow the resolution at [https://devanswers.co/problem-php-7-2-phpmyadmin-warning-in-librariessql-count/]
  * connect again via "ssh"
  * sudo cp /usr/share/phpmyadmin/libraries/sql.lib.php /usr/share/phpmyadmin/libraries/sql.lib.php.bak
  * sudo nano /usr/share/phpmyadmin/libraries/sql.lib.php
  * (search using Ctrl-W for "(count($analyzed_sql_results[‚select_expr‘] == 1)")
  * (replace found text with "((count($analyzed_sql_results[‚select_expr‘]) == 1)")
  * save and exit using Ctrl-X Y ENTER
