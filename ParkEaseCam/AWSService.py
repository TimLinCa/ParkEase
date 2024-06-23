import os
import boto3
from dotenv import load_dotenv
load_dotenv()

class AwsParameterManager(object):
    def __init__(self) -> None:
        self.client = boto3.client(
            service_name='ssm',
            aws_access_key_id = os.getenv('AWS_AccessKey'),
            aws_secret_access_key = os.getenv('AWS_SecretKey'),
            region_name=os.environ.get('AWS_RegionName')
        )

    def get_parameters(self,parameterName):
        response = self.client.get_parameters(
            Names=[parameterName])
        for parameter in response['Parameters']:
            return parameter['Value']