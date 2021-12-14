## Setup Instructions for Ubuntu 18.04

#### Install Dotnet

`sudo apt-get install dotnet-sdk-5.0`

If you get error that above dotnet-sdk package cannot be found, you might need to run this:
 
`sudo wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb`

`sudo dpkg -i packages-microsoft-prod.deb`

`sudo add-apt-repository universe`

`sudo apt-get update`

`sudo apt-get install apt-transport-https`

`sudo apt-get update`

`sudo apt-get install dotnet-sdk-5.0`  

#### Clone the Repo and build the Bots

`sudo git clone --recursive https://github.com/nerva-project/bots.git`

`cd bots`

`sudo dotnet restore`

`sudo dotnet build -c Release`

`sudo dotnet publish -c Release`

Binaries will be found at `~/bots/Bin/Release/publish`

#### Get your bot token and create your keyfile

Go to Discord Developers Portal and register a bot and grab the token. 

Below is an example of a keyfile.  Edit this with the required info.  Save it as `keys`

```
pass="This is the password required by the bots"
key_pass=${pass} # Specify a new password for maximum security. This password is required to decrypt the fusion key file
fusion_pid_key=""
fusion_donate_wallet_key=""
fusion_user_wallet_key=""

atom_token=""
fusion_token=""
```

The file should be saved with the binaries in `Bin/Release/publish`


#### Now Create a directory in the same Binary directory called 'Wallets'
You need 2 json files here as well as 2 wallets.  user.json and donation.json / user.wallet and donation.wallet

json files actually need to be not in wallets directory but in main Bots directory

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

The address and index should be the Main Address of each corresponding wallet.  If you want to add different addresses so users can donate for different things add corresponding subaddresses (Accounts).  


You also need `keyfile` in the directory with binaries.  It needs to have 3 lines encrypted using StringEncryptor.exe found here: https://github.com/nerva-project/tools/tree/master/Src/StringEncrypter

First line is encrypted password for Donate Wallet

Second line is encrypted password for User Wallet

Third line is encrypted fusion_pid_key???

See history of this README file for example what it needs to look like

#### Now you need to start 2 RPC Instances 

User Wallet:

`./nerva-wallet-rpc --rpc-bind-port 9995 --password mjks --disable-rpc-login --wallet-dir ~/bots/Bin/Release/publish/Wallets`

Donate Wallet:

`./nerva-wallet-rpc --rpc-bind-port 9996 --password mjks --disable-rpc-login --wallet-dir ~/bots/Bin/Release/publish/Wallets`

#### Now start the Bot

Fusion Bot:
`./Nerva.Bots.dll --bot Fusion.dll --token <bot-token> --donation-wallet-file donation --donation-wallet-port 9996 --user-wallet-file user --user-wallet-port 9995 --key-file keyfile`

`<bot-token>` needs to be encrypted using StringEncryptor.exe

Atom Bot:
`./Nerva.Bots --bot Atom.dll --token <bot-token> --key-file keyfile`

`<bot-token>` needs to be encrypted using StringEncryptor.exe

## The Bots are dependent on the Nerva PHP RPC API

### Install API On Node

#### Install Apache2 PHP and the Neede Addons

`sudo apt-get install apache2 php libapache2-mod-php php-curl`

#### clone the api into `/var/www/html/api`

`https://bitbucket.org/nerva-project/nerva.rpc.php`

#### Restart Apache

Visit http://nodeip/api to see if its up and working.   Then close port 80 if the node so the web server isnt web facing. 