## Setup Instructions for Ubuntu 18.04

#### Install Dotnet

`sudo add-apt-repository universe #Probably already have`

`sudo apt-get update`

`sudo apt-get install apt-transport-https #Most likeyalready installed`

`sudo apt-get update`

`sudo apt-get install dotnet-sdk-2.2  #Bots use 2.2 not 3.1`

#### Clone the Repo and build the Bots

`git clone --recursive https://bitbucket.org/nerva-project/bots.git`

`cd bots`

`dotnet restore`

`dotnet build -c Release`

`dotnet publish -c Release`

Binaries will be found at `~/bots/Bin/Release/publish`

#### Create your encrypted tokens

Go to Discord Developers Portal and register a bot and grab the token. Use the encrypt-tokens script below to create data required for a Keyfile.  Make Sure it is in the same directory as StringEncrypter.dll

```
#!/bin/bash
dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

#This is the master password. 
pass=""

#Bot token
bot_token=""

#This key is used to encrypt payment id's that users use to identify themselves as donors
pid_key=""

#this is the password used to create the donation wallet
donate_wallet_key=""

#this is the password used to create the user wallet
user_wallet_key="YEG42b96"

echo "Bot Token:"
dotnet ./StringEncrypter.dll --password ${pass} --encrypt ${bot_token}

echo "==========================================="
echo "Key file contents"
echo "-------------------------------------------"
dotnet ./StringEncrypter.dll --password ${pass} --encrypt ${pid_key}
dotnet ./StringEncrypter.dll --password ${pass} --encrypt ${donate_wallet_key}
dotnet ./StringEncrypter.dll --password ${pass} --encrypt ${user_wallet_key}
echo "-------------------------------------------"
```
#### Output will look like this 

```
Bot Token:
2ZsXr5Tl3uiHWOMKxnjdC+Cnz1l911zLUf7owLxwvh1iQJV2EBcEMulNkzhXR7PqqAzIW0k143et11VvZCoMF09fiYtOHv55js9+CKmMfcMilaLrCiDXOVqwpR18JZQ1
===========================================
Key file contents
-------------------------------------------
3fUxzrZrcJjue2zzkd/IEb/MjrmnQHcbLkuPyB+VkC6x0x1YHh4LasYdSFomRkmz
eOd92NwcCzZOiTiNC7esU8IGhyXYGxUx9plaM7YEE//5hwr+cfoaZIEYun+MmOMj
6/dKCWNyfKE7S3R2eZqOx5F/SHh3YyGcqKyLC9QWEL6cj4Q3zsBzSVMyMkWflsU8
-------------------------------------------
```

Take the Keyfile Contents and place in a file called 'keyfile' with the binaries in Bin/Release/publish

#### It's content should look like:

```
3fUxzrZrcJjue2zzkd/IEb/MjrmnQHcbLkuPyB+VkC6x0x1YHh4LasYdSFomRkmz
eOd92NwcCzZOiTiNC7esU8IGhyXYGxUx9plaM7YEE//5hwr+cfoaZIEYun+MmOMj
6/dKCWNyfKE7S3R2eZqOx5F/SHh3YyGcqKyLC9QWEL6cj4Q3zsBzSVMyMkWflsU8
```

#### Now Create a directory in the same Binary directory called 'Wallets'
You need 2 json files here as well as 2 wallets.  user.json and donation.json / user.wallet and donation.wallet

#### The json should look like:

```
{
    "accounts": [{
            "index": 0,
            "name": "user",
            "address": "NVCneXAGQhF2m2JgL97Te4bZjk2J3E1bbMZp92xrZJWtbiX6EzKv98dEoFuQ12ZTDjdbD6yWfv2DHEiAiuMtTJAci4L42f7C",
            "display": false
        }
    ]
}
```

The address and index should be the Main Address of each corresponding wallet.  If tou want to add different addresses 
so users can donate for different things add corresponding subaddresses (Accounts).  

#### Now you need to start 2 RPC Instances 

`./nerva-wallet-rpc --rpc-bind-port 9995 --password mjks --disable-rpc-login --wallet-dir ~/bots/Bin/Release/publish/Wallets/`

`./nerva-wallet-rpc --rpc-bind-port 9996 --password mjks --disable-rpc-login --wallet-dir ~/bots/Bin/Release/publish/Wallets/`

#### Now start the Bot

`./Nerva.Bots.dll --bot Fusion.dll --token <bot-token> --donation-wallet-file donation --donation-wallet-port 9996 --user-wallet-file user --user-wallet-port 9995 --key-file keyfile`

## The Bots are dependent on the Nerva PHP RPC API

### Install API On Node

#### Install Apache2 PHP and the Neede Addons

`sudo apt-get install apache2 php libapache2-mod-php php-curl`

#### clone the api into `/var/www/html/api`

`https://bitbucket.org/nerva-project/nerva.rpc.php`

#### Restart Apache

Visit http://nodeip/api to see if its up and working.   Then close port 80 if the node so the web server isnt web facing. 