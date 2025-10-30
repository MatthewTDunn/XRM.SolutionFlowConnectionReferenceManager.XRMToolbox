# XRMToolbox Plugin: Flow Connection Reference Reassigner

![GitHub Release](https://img.shields.io/github/v/release/MatthewTDunn/ConnectionReferenceReassignmentTool?style=flat-square)

## Overview

The **Flow Connection Reference Reassigner** is a [XRMToolBox](https://www.xrmtoolbox.com/) plugin developed to provide administrators and developers with a means to inspect & bulk reassign connection references on Power Automate flows. It is primarily designed to reduce the administrative burden of manually updating connection references from user-owned accounts to service principals or system accounts, aligning with governance best practices and organizational security policies.

Use this tool to:
- Audit connection references across multiple flows
- Efficiently reassign Power Automate Flow connection references within a solution or at an environment level

---

## Getting Started

- Open XRMToolBox and connect to a Dataverse environment
- Load this plugin from your local build or via the XRM Toolbox library
- Click the solution (or environment label if appropriate) you'd like to reassign

![Initial Solution Loading Step](/assets/Screenshot1.jpg)


![Executing a Flow Connection Reference Update Screenshot](/assets/Screenshot2.jpg)

### UI Reference

1. The replacement connection reference (this is filtered from connection references you own, with the same type).
2. How many flows will be impacted by the updating of this connection reference.
3. How many flow actions will be impacted by the updating of this connection reference.
4. The logical name of the replacement connection reference.
5. A read-only tab that provides insight into the particular actions associated with the connection reference list.
6. The type of connection reference.
7. Refresh the unmanaged solution/environment list.
8. **Execute the update of replacement connection references**.

---

## Licence
This project is licensed under the **MIT License** — see the [LICENCE](/LICENSE.txt) file for details.

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you’d like to change.