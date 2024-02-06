Feature: Comparison Service

  @ignore
  Scenario: Combines 4x4 records, keeping only first and last similar records matched:
    Given log records from profile 1:
      | DateTime                | Level | Thread  | File      | Logger  | Message             |
      | 2023-01-01 12:00:00.001 | INFO  | Main    | file2.log | Logger1 | Message 123         |
      | 2023-01-01 12:00:00.123 | INFO  | Thread2 | file1.log | Logger1 | Another Message     |
      | 2023-01-01 12:00:01.376 | INFO  | Thread3 | file1.log | Logger1 | Yet Another Message |
      | 2023-01-01 12:00:01.465 | INFO  | Thread4 | file1.log | Logger2 | Final Message       |
    And log records from profile 2:
      | DateTime                | Level | Thread  | File      | Logger  | Message                            |
      | 2023-01-01 12:30:00.001 | INFO  | Main    | file2.log | Logger1 | Message 123                        |
      | 2023-01-01 12:30:00.123 | INFO  | Thread2 | file1.log | Logger1 | Another Message Second Profile     |
      | 2023-01-01 12:30:00.376 | INFO  | Thread3 | file1.log | Logger1 | Yet Another Message Second Profile |
      | 2023-01-01 12:30:00.465 | INFO  | Thread4 | file1.log | Logger2 | Final Message                      |
    When comparing profiles
    Then the result is the following:
      | Record1 | Record2 |
      | 1       | 1       |
      | 2       |         |
      | 3       |         |
      |         | 2       |
      |         | 3       |
      | 4       | 4       |

  @ignore
  Scenario: Combines 3x2 records
    Given log records from profile 1:
      | DateTime                | Level | Thread  | File      | Logger  | Message         |
      | 2023-01-01 12:00:00.001 | INFO  | Main    | file2.log | Logger1 | Message 123     |
      | 2023-01-01 12:00:00.123 | INFO  | Thread2 | file1.log | Logger1 | Another Message |
      | 2023-01-01 12:00:03.465 | INFO  | Thread4 | file1.log | Logger2 | Final Message   |
    And log records from profile 2:
      | DateTime                | Level | Thread  | File      | Logger  | Message         |
      | 2023-01-01 12:30:00.001 | INFO  | Main    | file2.log | Logger1 | Message 123     |
      | 2023-01-01 12:30:00.555 | INFO  | Thread2 | file1.log | Logger1 | Another Message |
    When comparing profiles
    Then the result is the following:
      | Record1 | Record2 |
      | 1       | 1       |
      | 2       | 2       |
      | 3       |         |

  @ignore
  Scenario: Combines 2x3 records
    Given log records from profile 1:
      | DateTime                | Level | Thread  | File      | Logger  | Message         |
      | 2023-01-01 12:00:00.001 | INFO  | Main    | file2.log | Logger1 | Message 123     |
      | 2023-01-01 12:00:00.123 | INFO  | Thread2 | file1.log | Logger1 | Another Message |
    And log records from profile 2:
      | DateTime                | Level | Thread  | File      | Logger  | Message         |
      | 2023-01-01 12:30:00.001 | INFO  | Main    | file2.log | Logger1 | Message 123     |
      | 2023-01-01 12:30:00.555 | INFO  | Thread2 | file1.log | Logger1 | Another Message |
      | 2023-01-01 12:30:03.465 | INFO  | Thread4 | file1.log | Logger2 | Final Message   |
    When comparing profiles
    Then the result is the following:
      | Record1 | Record2 |
      | 1       | 1       |
      | 2       | 2       |
      |         | 3       |
