docker run -p 3306:3306 --name some-mysql -e MYSQL_ROOT_PASSWORD=steeltoe -d mysql:5.6
docker ps 
(find container id)
docker exec -it <containerid> bash

 mysql -u root -p
 mysql> create database steeltoe;
 mysql> grant all privileges on steeltoe.* to 'steeltoe'@'%';
 mysql> grant all privileges on steeltoe.* to 'steeltoe'@'%' IDENTIFIED BY 'steeltoe';
 mysql> FLUSH PRIVILEGES

