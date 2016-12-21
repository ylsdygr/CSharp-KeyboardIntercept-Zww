-- MySQL Script generated by MySQL Workbench
-- Mon Dec 19 20:19:43 2016
-- Model: New Model    Version: 1.0
-- MySQL Workbench Forward Engineering

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='TRADITIONAL,ALLOW_INVALID_DATES';

-- -----------------------------------------------------
-- Schema mydb
-- -----------------------------------------------------
-- -----------------------------------------------------
-- Schema keyboardAuth
-- -----------------------------------------------------

-- -----------------------------------------------------
-- Schema keyboardAuth
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `keyboardAuth` DEFAULT CHARACTER SET utf8 ;
USE `keyboardAuth` ;

-- -----------------------------------------------------
-- Table `keyboardAuth`.`authorized_lists`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `keyboardAuth`.`authorized_lists` (
  `number` INT(11) UNSIGNED NOT NULL,
  `user_name` VARCHAR(20) NOT NULL,
  `key_serial` VARCHAR(200) NOT NULL,
  `key_code` VARCHAR(128) NULL DEFAULT NULL,
  `used_counts` VARCHAR(128) NULL DEFAULT NULL,
  `update_time` DATETIME NULL DEFAULT NULL,
  UNIQUE INDEX `user_name_UNIQUE` (`user_name` ASC),
  UNIQUE INDEX `key_code_UNIQUE` (`key_code` ASC))
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8
COMMENT = 'This table use to store authorized keys';


-- -----------------------------------------------------
-- Table `keyboardAuth`.`used_log`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `keyboardAuth`.`used_log` (
  `number` INT(11) NOT NULL AUTO_INCREMENT,
  `user` VARCHAR(30) NOT NULL,
  `start_use_time` DATETIME NULL DEFAULT NULL,
  `end_use_time` DATETIME NULL DEFAULT NULL,
  `ip_address` VARCHAR(20) NULL DEFAULT NULL,
  `computer_name` VARCHAR(50) NULL DEFAULT NULL,
  `computer_username` VARCHAR(50) NULL DEFAULT NULL,
  `used_counts` INT(20) UNSIGNED NOT NULL,
  PRIMARY KEY (`number`))
ENGINE = InnoDB
AUTO_INCREMENT = 62
DEFAULT CHARACTER SET = utf8
COMMENT = 'This table use ed store the history of authorizatio.';


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
