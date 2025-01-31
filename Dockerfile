FROM php:8.2-apache

COPY ./website /var/www/html/

RUN docker-php-ext-install pdo_mysql

EXPOSE 80